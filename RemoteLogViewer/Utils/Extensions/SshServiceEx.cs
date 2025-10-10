namespace RemoteLogViewer.Utils.Extensions;

public static class SshServiceEx {
	/// <summary>
	/// SSH サーバー上のディレクトリを一覧表示します。
	/// </summary>
	/// <param name="sshService">SSHサービス</param>
	/// <param name="path">パス</param>
	/// <returns></returns>
	public static string[] ListDirectory(this Services.SshService sshService, string path) {
		var output = sshService.Run($"ls -A \"{path.Replace("\"", "\\\"")}\"");
		return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}
}
