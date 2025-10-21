using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.Utils;

public static class PathUtils {
	public static string CombineUnixPath(string path1, string path2, FileSystemObjectType fsoType) {
		if (path2.StartsWith('/')) {
			return path2;
		}
		return path1.TrimEnd('/') + "/" + path2 + (fsoType == FileSystemObjectType.Directory || fsoType == FileSystemObjectType.SymlinkDirectory ? "/" : "");
	}
}
