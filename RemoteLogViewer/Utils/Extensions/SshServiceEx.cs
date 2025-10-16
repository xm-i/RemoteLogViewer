using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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
	///     リモートファイルの総行数を取得します。<br/>
	///     wc を利用して高速に最終行番号 (行数) を取得します。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイルのパス。</param>
	/// <returns>総行数。</returns>
	/// <exception cref="InvalidOperationException">SSH 未接続の場合。</exception>
	/// <exception cref="ArgumentException">パスが無効な場合。</exception>
	public static long GetLineCount(this SshService sshService, string remoteFilePath) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			throw new ArgumentException("file path is empty", nameof(remoteFilePath));
		}
		var escaped = EscapeSingleQuotes(remoteFilePath);
		var output = sshService.Run($"wc -l '{escaped}' 2>/dev/null | awk '{{print $1}}'").Trim();
		if (string.IsNullOrEmpty(output)) {
			return 0;
		}
		if (long.TryParse(output, out var count)) {
			return count;
		}
		throw new InvalidOperationException($"行数取得に失敗しました: {output}");
	}

	/// <summary>
	///     指定した開始行から終了行までの行を取得します。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイルのパス。</param>
	/// <param name="startLine">開始行 (1 始まり)。</param>
	/// <param name="endLine">終了行 (1 始まり)。</param>
	/// <param name="fileEncoding">ソースエンコーディング。</param>
	/// <returns>取得行。</returns>
	public static IEnumerable<TextLine> GetLines(this SshService sshService, string remoteFilePath, long startLine, long endLine, string? fileEncoding) {
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
		var escaped = EscapeSingleQuotes(remoteFilePath);
		var convertPipe = NeedsConversion(fileEncoding, sshService.IconvEncoding) ? $" | iconv -f {EscapeSingleQuotes(fileEncoding!)} -t {sshService.IconvEncoding}//IGNORE" : string.Empty;
		var output = sshService.Run($"LC_ALL=C sed -n '{startLine},{endLine}p' '{escaped}' 2>/dev/null" + convertPipe);
		if (string.IsNullOrEmpty(output)) {
			return Array.Empty<TextLine>();
		}

		var lines = output.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

		// sed は末尾改行があると空行が最後にできるため除外
		if (lines.Length > 0 && string.IsNullOrEmpty(lines[^1])) {
			lines = lines[..^1];
		}

		// 実際の行番号を付けて返す
		return lines.Select((content, i) => new TextLine(startLine + i, content));
	}

	/// <summary>
	///     grep 検索 を行います。
	/// </summary>
	/// <param name="sshService">SSH サービス。</param>
	/// <param name="remoteFilePath">対象ファイル。</param>
	/// <param name="pattern">パターン。</param>
	/// <param name="maxResults">最大件数。</param>
	/// <param name="ignoreCase">大文字小文字無視。</param>
	/// <param name="fileEncoding">ファイルエンコーディング。</param>
	/// <returns>一致行。</returns>
	public static TextLine[] Grep(this SshService sshService, string remoteFilePath, string pattern, int maxResults, bool ignoreCase, string? fileEncoding) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			throw new ArgumentException("file path is empty", nameof(remoteFilePath));
		}
		if (sshService.IconvEncoding == null) {
			throw new InvalidOperationException("Iconv Encoding not found.");
		}
		pattern ??= string.Empty;
		pattern = pattern.Trim();
		if (pattern.Length == 0) {
			return Array.Empty<TextLine>();
		}
		if (maxResults < 1) {
			throw new ArgumentException("maxResults must be >=1", nameof(maxResults));
		}

		var escapedPath = EscapeSingleQuotes(remoteFilePath);
		var escapedPattern = EscapeSingleQuotes(pattern);
		var ic = ignoreCase ? " -i" : string.Empty;

		var convertPipe = NeedsConversion(fileEncoding, sshService.IconvEncoding) ? $" | iconv -f {EscapeSingleQuotes(fileEncoding!)} -t {EscapeSingleQuotes(sshService.IconvEncoding)}//IGNORE" : string.Empty;
		var cmd = $"LC_ALL=C grep -n -h -m {maxResults}{ic} -F --binary-files=text --color=never -- '{escapedPattern}' -- '{escapedPath}' 2>/dev/null" + convertPipe + " || true";
		var output = sshService.Run(cmd);
		if (string.IsNullOrWhiteSpace(output)) {
			return Array.Empty<TextLine>();
		}
		var list = new List<TextLine>();
		var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		foreach (var line in lines) {
			var idx = line.IndexOf(':');
			if (idx <= 0) {
				continue;
			}
			if (!long.TryParse(line[..idx], out var ln)) {
				continue;
			}
			var content = line[(idx + 1)..];
			list.Add(new TextLine(ln, content));
		}
		return [.. list];
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
			return false; // 指定なしは 変換不要扱い
		}
		return !encoding1.Equals(encoding2, StringComparison.OrdinalIgnoreCase);
	}
}
