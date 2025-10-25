using System.Collections.Generic;
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
	/// <param name="sshService">SSHサービス。</param>
	/// <param name="path">対象ディレクトリのパス。</param>
	/// <returns>ディレクトリエントリ一覧。</returns>
	public static FileSystemObject[] ListDirectory(this SshService sshService, string path) {
		var escapedPath = path.Replace("\"", "\\\"");
		var awk =
			"/^total / { next } " +
			"/^l/ { target=$NF; testpath=substr(target,1,1)==\"/\"?target:P\"/\"target; if (system(\"[ -d \" testpath \" ]\")==0) code=4; else code=3; print code, $0; next } " +
			"/^d/ { print 2, $0; next } " +
			"/^-/ { print 1, $0; next } " +
			"{ print 0, $0 }";
		var cmd = $"ls -al --time-style=+%Y-%m-%dT%H:%M:%S%z \"{escapedPath}\" | awk -v P=\"{escapedPath}\" '{awk}'";
		var output = sshService.Run(cmd);
		var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		var list = new List<FileSystemObject>();
		// 4 lrwxrwxrwx   1 root root          7 2024-04-22T22:08:03+0900 bin -> usr/bin
		// 2 drwxr-xr-x   2 root root       4096 2024-02-26T21:58:31+0900 bin.usr-is-merged
		// 1 -rw-------   1 root root 3028287488 2024-07-12T15:31:21+0900 swap.img
		foreach (var line in lines) {
			var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			// code perms links owner group size ts name...
			if (parts.Length < 8) {
				continue;
			}
			var code = parts[0];
			var sizeStr = parts[5];
			var ts = parts[6];
			var nameWithMaybeLink = string.Join(' ', parts.Skip(7));
			var fileName = nameWithMaybeLink;
			var arrowIndex = nameWithMaybeLink.IndexOf(" -> ", StringComparison.Ordinal);
			if (arrowIndex >= 0) {
				fileName = nameWithMaybeLink[..arrowIndex];
			}
			if (fileName == ".") {
				continue;
			}

			var fsoType = code switch {
				"2" => FileSystemObjectType.Directory,
				"4" => FileSystemObjectType.SymlinkDirectory,
				"3" => FileSystemObjectType.SymlinkFile,
				_ => FileSystemObjectType.File
			};

			ulong.TryParse(sizeStr, out var size);

			var normalizedTs = ts.Insert(ts.Length - 2, ":");
			DateTime? lastUpdated = null;
			if (DateTimeOffset.TryParseExact(normalizedTs, "yyyy-MM-dd'T'HH:mm:ssK", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto)) {
				lastUpdated = dto.UtcDateTime;
			}
			list.Add(new FileSystemObject(path, fileName, fsoType, size, lastUpdated));
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
	public static async IAsyncEnumerable<TextLine> GetLinesAsync(this SshService sshService, string remoteFilePath, long startLine, long endLine, string? fileEncoding, ByteOffset byteOffset, [EnumeratorCancellation] CancellationToken ct) {
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
		var startBytes = byteOffset.Bytes + 1;
		command = $"LC_ALL=C tail -c +{startBytes} '{escaped}' 2>/dev/null | sed -n '{relativeStart},{relativeEnd}p;{relativeEnd}q' 2>/dev/null" + convertPipe;

		var lines = sshService.RunAsync(command, ct);
		await foreach (var line in lines.Select((content, i) => new TextLine(startLine + i, content))) {
			if (ct.IsCancellationRequested) {
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
	public static async IAsyncEnumerable<TextLine> GrepAsync(this SshService sshService, string remoteFilePath, string pattern, bool ignoreCase, string? fileEncoding, [EnumeratorCancellation] CancellationToken ct) {
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
		var lines = sshService.RunAsync(cmd, ct);

		await foreach (var line in lines) {
			if (ct.IsCancellationRequested) {
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
	/// <param name="ct">キャンセルトークン</param>
	/// <returns>インデックス列挙。</returns>
	public static async IAsyncEnumerable<ByteOffset> CreateByteOffsetMap(this SshService sshService, string remoteFilePath, int interval, [EnumeratorCancellation] CancellationToken ct) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			throw new ArgumentException("file path is empty", nameof(remoteFilePath));
		}

		if (interval < 1) {
			throw new ArgumentException("interval must be >=1", nameof(interval));
		}
		var escapedPath = EscapeSingleQuotes(remoteFilePath);
		var cmd = $"LC_ALL=C awk '{{ offset+=length($0)+1 }} NR%{interval}==0 {{ print NR, offset }} END {{ if (NR%{interval} != 0) print NR, offset }}' '{escapedPath}' 2>/dev/null";
		var lines = sshService.RunAsync(cmd, ct);

		await foreach (var line in lines) {
			if (ct.IsCancellationRequested) {
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
	///既存のバイトオフセット (<paramref name="startOffset"/>)から指定行番号 (<paramref name="targetLine"/>)まで追加で読み込まれるバイト数を AWKで算出し、新しい <see cref="ByteOffset"/> を返します。
	/// startOffset.LineNumber 行までの内容が既に取得済みである前提で、tail -c +{startOffset.Bytes+1}で残りを読み進めます。
	/// </summary>
	/// <param name="sshService">SSH サービス。</param>
	/// <param name="remoteFilePath">対象ファイルパス。</param>
	/// <param name="startOffset">開始バイトオフセット。</param>
	/// <param name="targetLine">新規に算出したい行番号 (startOffset.LineNumber以上)。</param>
	/// <param name="ct">キャンセルトークン。</param>
	/// <returns>対象行までの累積バイトオフセット。</returns>
	public static async Task<ByteOffset> CreateByteOffsetUntilLineAsync(this SshService sshService, string remoteFilePath, ByteOffset startOffset, long targetLine, CancellationToken ct) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			throw new ArgumentException("file path is empty", nameof(remoteFilePath));
		}
		if (targetLine < startOffset.LineNumber) {
			throw new ArgumentException("targetLine must be >= startOffset.LineNumber", nameof(targetLine));
		}
		if (targetLine == startOffset.LineNumber) {
			return startOffset;
		}
		var escaped = EscapeSingleQuotes(remoteFilePath);
		var relativeLines = targetLine - startOffset.LineNumber;
		var startBytes = startOffset.Bytes + 1;
		var cmd = $"LC_ALL=C tail -c +{startBytes} '{escaped}' 2>/dev/null | awk -v base={startOffset.Bytes} -v maxLines={relativeLines} '{{ base+=length($0)+1; if (NR==maxLines) {{ print base; exit }} }} END {{ if (NR<maxLines) print base }}' 2>/dev/null";
		var lines = await sshService.RunAsync(cmd, ct).ToArrayAsync();
		if (lines.Length != 1) {
			throw new InvalidOperationException("Failed to calculate byte offset.");
		}
		if (!ulong.TryParse(lines[0].Trim(), out var parsed)) {
			throw new InvalidCastException("Failed to parse byte offset.");
		}

		return new ByteOffset(targetLine, parsed);
	}

	/// <summary>
	/// ファイル末尾の新規追記行を取得します。起点バイトオフセット以降の内容をtail -fで追跡し、既存最終行番号以前の行はスキップします。
	/// </summary>
	/// <param name="sshService">SSH サービス。</param>
	/// <param name="remoteFilePath">対象ファイル。</param>
	/// <param name="fileEncoding">ファイルエンコーディング。</param>
	/// <param name="startOffset">開始オフセット。</param>
	/// <param name="currentLastLine">現在取得済み最終行番号。</param>
	/// <param name="ct">キャンセルトークン。</param>
	public static async IAsyncEnumerable<TextLine> TailFollowAsync(this SshService sshService, string remoteFilePath, string? fileEncoding, ByteOffset startOffset, long currentLastLine, [EnumeratorCancellation] CancellationToken ct) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			yield break;
		}
		if (sshService.IconvEncoding == null) {
			yield break;
		}
		var escaped = EscapeSingleQuotes(remoteFilePath);
		var convertPipe = NeedsConversion(fileEncoding, sshService.IconvEncoding) ? $" | iconv -f {EscapeSingleQuotes(fileEncoding!)} -t {sshService.IconvEncoding}//IGNORE" : string.Empty;
		var startBytes = startOffset.Bytes + 1; // startOffset.LineNumberまで読み込まれている前提
		var cmd = $"tail -c +{startBytes} -f '{escaped}' 2>/dev/null" + convertPipe;
		var nextLine = startOffset.LineNumber + 1;
		await foreach (var line in sshService.RunAsync(cmd, ct)) {
			if (ct.IsCancellationRequested) {
				yield break;
			}
			var ln = nextLine++;
			if (ln <= currentLastLine) {
				continue;
			} //既存行はスキップ
			yield return new TextLine(ln, line, true);
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
