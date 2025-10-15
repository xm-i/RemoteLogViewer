namespace RemoteLogViewer.Utils;

public static class PathUtils {
	public static string CombineUnixPath(string path1, string path2) {
		if (path2.StartsWith('/')) {
			return path2;
		}

		return path1 == "/" ? "/" + path2 : path1.TrimEnd('/') + "/" + path2;

	}
}
