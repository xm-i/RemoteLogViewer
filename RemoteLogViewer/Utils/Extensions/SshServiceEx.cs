using System.Collections.Generic;
using System.Globalization;
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
}
