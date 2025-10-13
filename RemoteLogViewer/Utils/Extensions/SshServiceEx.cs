using System.Collections.Generic;
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
	/// <returns></returns>
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

			list.Add(new FileSystemObject(fileName, type, size, lastUpdated));
		}

		return [.. list];
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
	///     指定した開始行から終了行まで (両端含む) の行を取得します。<br/>
	///     sed の範囲指定 (start,endp) を使用します。
	/// </summary>
	/// <param name="remoteFilePath">対象ファイルのパス。</param>
	/// <param name="startLine">開始行 (1 始まり)。</param>
	/// <param name="endLine">終了行 (1 始まり, 開始行以上)。</param>
	/// <returns>
	///     取得した行の <see cref="TextLine"/> コレクション。<br/>
	///     存在しない行番号は無視されます。
	/// </returns>
	/// <exception cref="ArgumentException">パラメータが無効な場合。</exception>
	/// <exception cref="InvalidOperationException">SSH 未接続の場合。</exception>
	public static IEnumerable<TextLine> GetLines(this SshService sshService, string remoteFilePath, long startLine, long endLine) {
		if (string.IsNullOrWhiteSpace(remoteFilePath)) {
			throw new ArgumentException("File path is empty.", nameof(remoteFilePath));
		}
		if (startLine < 1) {
			throw new ArgumentException("startLine must be >= 1.", nameof(startLine));
		}
		if (endLine < startLine) {
			throw new ArgumentException("endLine must be >= startLine.", nameof(endLine));
		}

		var escaped = EscapeSingleQuotes(remoteFilePath);

		var output = sshService.Run($"sed -n '{startLine},{endLine}p' '{escaped}' 2>/dev/null");

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
	///     シェルのシングルクォートで囲むためにパス内のシングルクォートをエスケープします。
	/// </summary>
	/// <param name="path">パス。</param>
	/// <returns>エスケープ後のパス。</returns>
	private static string EscapeSingleQuotes(string path) {
		return path.Replace("'", "'\\''");
	}
}
