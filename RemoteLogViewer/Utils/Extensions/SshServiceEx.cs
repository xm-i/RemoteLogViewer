using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;

using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.Utils.Extensions;

public static class SshServiceEx {
	/// <summary>
	/// SSH サーバー上のディレクトリを一覧表示します。
	/// </summary>
	/// <param name="sshService">SSHサービス</param>
	/// <param name="path">パス</param>
	/// <returns>ディレクトリエントリ一覧。</returns>
	public static FileSystemObject[] ListDirectory(this SshService sshService, string path) {
		var escaped = path.Replace("\"", "\\\"");
		var output = sshService.Run($"ls -al --time-style=+%Y-%m-%dT%H:%M:%S%z \"{escaped}\"");

		var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var list = new List<FileSystemObject>();
		// lrwxrwxrwx   1 root root          7 2024-04-22T22:08:03+0900 bin -> usr/bin
		// drwxr-xr-x   2 root root       4096 2024-02-26T21:58:31+0900 bin.usr-is-merged
		// -rw-------   1 root root 3028287488 2024-07-12T15:31:21+0900 swap.img
		foreach (var line in lines) {
			if (line.StartsWith("total ", StringComparison.OrdinalIgnoreCase)) {
				continue;
			}

			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			if (parts.Length < 7) {
				continue;
			}

			var perms = parts[0];
			var sizeStr = parts[4];
			var ts = parts[5];
			var nameWithMaybeLink = string.Join(' ', parts.Skip(6));

			var fileName = nameWithMaybeLink;
			var arrowIndex = nameWithMaybeLink.IndexOf(" -> ", StringComparison.Ordinal);
			if (arrowIndex >= 0) {
				fileName = nameWithMaybeLink[..arrowIndex];
			}
			if (fileName == ".") {
				continue;
			}

			FileSystemObjectType? type = null;
			if (perms.Length > 0) {
				type = perms[0] switch {
					'd' => FileSystemObjectType.Directory,
					'l' => FileSystemObjectType.Symlink,
					_ => FileSystemObjectType.File
				};
			}

			long.TryParse(sizeStr, out var size);

			var normalizedTs = ts.Insert(ts.Length - 2, ":");
			DateTime? lastUpdated = null;
			if (DateTimeOffset.TryParseExact(normalizedTs, "yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto)) {
				lastUpdated = dto.UtcDateTime;
			}

			list.Add(new FileSystemObject(path, fileName, type, size, lastUpdated));
		}

		return [.. list];
	}

	/// <summary>
	///     リモート環境で利用可能な iconv のエンコーディング一覧を取得します。
	/// </summary>
	/// <param name="sshService">SSH サービス。</param>
	/// <returns>エンコーディング名配列。</returns>
	public static string[] ListIconvEncodings(this SshService sshService) {
		var output = sshService.Run("iconv -l 2>/dev/null || true");
		var tokens = output.Split(['\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var t in tokens) {
			var name = t.Trim();
			if (name.Length == 0) {
				continue;
			}
			if (name.EndsWith("//", StringComparison.Ordinal)) {
				name = name[..^2];
			}
			set.Add(name);
		}
		return set.Where(x => Constants.EncodingPairs.Any(ep => ep.Iconv == x)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
	}

	/// <summary>
	///     指定した開始行から終了行までの行を取得します。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイルのパス。</param>
	/// <param name="startLine">開始行 (1 始まり)。</param>
	/// <param name="endLine">終了行 (1 始まり)。</param>
	/// <param name="fileEncoding">ソースエンコーディング。</param>
	/// <returns>取得行。</returns>
	public static async IAsyncEnumerable<TextLine> GetLinesAsync(this SshService sshService, string remoteFilePath, long startLine, long endLine, string? fileEncoding, ByteOffset byteOffset, [EnumeratorCancellation] CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			throw new ArgumentException("File path is empty.", nameof(remoteFilePath));
		}
		if (startLine < 1) {
			throw new ArgumentException("startLine must be >= 1.", nameof(startLine));
		}
		if (endLine < startLine) {
			throw new ArgumentException("endLine must be >= startLine.", nameof(endLine));
		}
		if (sshService.IconvEncoding == null) {
			throw new InvalidOperationException("Iconv Encoding not found.");
		}
		if (byteOffset.LineNumber > startLine) {
			throw new ArgumentException("byteOffset.LineNumber is less than startLine", nameof(byteOffset));
		}
		var escaped = EscapeSingleQuotes(remoteFilePath);
		var convertPipe = NeedsConversion(fileEncoding, sshService.IconvEncoding) ? $" | iconv -f {EscapeSingleQuotes(fileEncoding!)} -t {sshService.IconvEncoding}//IGNORE" : string.Empty;

		string command;
		var relativeStart = startLine - byteOffset.LineNumber;
		var relativeEnd = endLine - byteOffset.LineNumber;
		var startBytes = byteOffset.bytes + 1;
		command = $"LC_ALL=C tail -c +{startBytes} '{escaped}' 2>/dev/null | sed -n '{relativeStart},{relativeEnd}p;{relativeEnd}q' 2>/dev/null" + convertPipe;

		var lines = sshService.RunAsync(command, cancellationToken);
		await foreach (var line in lines.Select((content, i) => new TextLine(startLine + i, content))) {
			try {
				cancellationToken.ThrowIfCancellationRequested();
			} catch (OperationCanceledException) {
				yield break;
			}
			yield return line;
		}
	}

	/// <summary>
	///     grep 検索 を行います。
	/// </summary>
	/// <param name="sshService">SSH サービス。</param>
	/// <param name="remoteFilePath">対象ファイル。</param>
	/// <param name="pattern">パターン。</param>
	/// <param name="ignoreCase">大文字小文字無視。</param>
	/// <param name="fileEncoding">ファイルエンコーディング。</param>
	/// <param name="fileEncoding">ソースエンコーディング。</param>
	/// <returns>一致行。</returns>
	public static async IAsyncEnumerable<TextLine> GrepAsync(this SshService sshService, string remoteFilePath, string pattern, bool ignoreCase, string? fileEncoding, [EnumeratorCancellation] CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			throw new ArgumentException("file path is empty", nameof(remoteFilePath));
		}
		if (sshService.IconvEncoding == null) {
			throw new InvalidOperationException("Iconv Encoding not found.");
		}
		pattern ??= string.Empty;
		pattern = pattern.Trim();
		if (pattern.Length == 0) {
			yield break;
		}

		var escapedPath = EscapeSingleQuotes(remoteFilePath);
		var escapedPattern = EscapeSingleQuotes(pattern);
		var ic = ignoreCase ? " -i" : string.Empty;

		// パターン変換必要か
		var needsPatternConversion = NeedsConversion(sshService.IconvEncoding, fileEncoding);
		string patternExpr;
		if (needsPatternConversion && !string.IsNullOrWhiteSpace(fileEncoding)) {
			// command substitution を展開させるため、シングルクォートで囲まない
			patternExpr = "$(printf '%s' '" + escapedPattern + "' | iconv -f " + EscapeSingleQuotes(sshService.IconvEncoding) + " -t " + EscapeSingleQuotes(fileEncoding!) + " 2>/dev/null)";
		} else {
			// 変換不要: 安全にシングルクォートで囲む
			patternExpr = "'" + escapedPattern + "'";
		}

		var convertPipe = NeedsConversion(fileEncoding, sshService.IconvEncoding) ? " | iconv -f " + EscapeSingleQuotes(fileEncoding!) + " -t " + EscapeSingleQuotes(sshService.IconvEncoding) + "//IGNORE" : string.Empty;
		var cmd = $"LC_ALL=C grep -n -h {ic} -F --binary-files=text --color=never --line-buffered -- {patternExpr} -- '{escapedPath}' 2>/dev/null{convertPipe} || true";
		var lines = sshService.RunAsync(cmd, cancellationToken);
		
		await foreach (var line in lines) {
			try {
				cancellationToken.ThrowIfCancellationRequested();
			} catch (OperationCanceledException) {
				yield break;
			}
			var idx = line.IndexOf(':');
			if (idx <= 0) {
				continue;
			}
			if (!long.TryParse(line[..idx], out var ln)) {
				continue;
			}
			var content = line[(idx + 1)..];
			yield return new TextLine(ln, content);
		}
	}

	/// <summary>
	///     大規模ファイル向け: 行番号からおおよそのバイトオフセットを取得するためのインデックスを一定間隔で作成します。
	///     返されるByteOffset は <paramref name="remoteFilePath"/> 先頭からのオフセット (改行含む) です。
	///     最終行後(EOF) の行番号 + 1 と最終オフセットも出力されます。
	/// </summary>
	/// <param name="sshService">SSH サービス。</param>
	/// <param name="remoteFilePath">対象ファイル 。</param>
	/// <param name="interval">インデックス間隔行数。1 以上。</param>
	/// <param name="cancellationToken">キャンセルトークン</param>
	/// <returns>インデックス列挙。</returns>
	public static async IAsyncEnumerable<ByteOffset> CreateByteOffsetMap(this SshService sshService, string remoteFilePath, int interval, [EnumeratorCancellation] CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			throw new ArgumentException("file path is empty", nameof(remoteFilePath));
		}

		if (interval < 1) {
			throw new ArgumentException("interval must be >=1", nameof(interval));
		}
		var escapedPath = EscapeSingleQuotes(remoteFilePath);
		var cmd = $"awk '{{ offset+=length($0)+1 }} NR%{interval}==0 {{ print NR, offset }} END {{ if (NR%{interval} != 0) print NR, offset }}' '{escapedPath}' 2>/dev/null";
		var lines = sshService.RunAsync(cmd, cancellationToken);

		await foreach (var line in lines) {
			try {
				cancellationToken.ThrowIfCancellationRequested();
			} catch (OperationCanceledException) {
				yield break;
			}
			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (parts.Length != 2) {
				continue;
			}
			if (!long.TryParse(parts[0], out var ln)) {
				continue;
			}
			if (!ulong.TryParse(parts[1], out var off)) {
				continue;
			}
			yield return new ByteOffset(ln, off);
		}
	}

	/// <summary>
	///     シェルのシングルクォートで囲むためにパス内のシングルクォートをエスケープします。
	/// </summary>
	/// <param name="path">パス。</param>
	/// <returns>エスケープ後のパス。</returns>
	private static string EscapeSingleQuotes(string path) {
		return path.Replace("'", "'\\''");
	}

	/// <summary>
	///     2つの文字コードが不一致の場合に変換が必要と判定します。
	/// </summary>
	/// <param name="sourceEncoding">ソースエンコーディング。</param>
	/// <returns>変換が必要なら true。</returns>
	private static bool NeedsConversion(string? encoding1, string? encoding2) {
		if (string.IsNullOrWhiteSpace(encoding1) || string.IsNullOrWhiteSpace(encoding2)) {
			return false;
		}
		return !encoding1.Equals(encoding2, StringComparison.OrdinalIgnoreCase);
	}
}
