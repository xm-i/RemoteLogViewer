using System.IO;

using RemoteLogViewer.Core.Services.Ssh;

namespace RemoteLogViewer.Core.Utils;

public static class PathUtils {
	public static string CombineUnixPath(string path1, string path2, FileSystemObjectType fsoType) {
		if (path2.StartsWith('/')) {
			return path2;
		}
		return path1.TrimEnd('/') + "/" + path2 + (fsoType == FileSystemObjectType.Directory || fsoType == FileSystemObjectType.SymlinkDirectory ? "/" : "");
	}

	public static string GetFileOrDirectoryName(string path) {
		return Path.GetFileName(path.TrimEnd('/'));
	}
}
