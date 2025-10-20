using System.IO;

namespace RemoteLogViewer.Services;

/// <summary>
/// ワークスペース(設定ファイル保存先)の管理サービス。
/// </summary>
[AddSingleton]
public class WorkspaceService {
	private readonly string _persistFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RemoteLogViewer", "WorkSpacePath.txt");

	/// <summary>選択済みワークスペースパス。未選択の場合は null。</summary>
	public string? WorkspacePath {
		get; private set;
	}

	public WorkspaceService() {
		try {
			if (File.Exists(this._persistFilePath)) {
				var path = File.ReadAllText(this._persistFilePath).Trim();
				if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) {
					this.WorkspacePath = path;
				}
			}
		} catch {
			// 読み込み失敗時は無視
		}
	}

	/// <summary>
	/// ワークスペースを設定します。
	/// </summary>
	/// <param name="path">ディレクトリパス。</param>
	/// <param name="persist">次回以降確認を省略する (保存する) 場合 true。</param>
	public void SetWorkspace(string path, bool persist) {
		this.WorkspacePath = path;
		if (persist) {
			try {
				Directory.CreateDirectory(Path.GetDirectoryName(this._persistFilePath)!);
				File.WriteAllText(this._persistFilePath, path);
			} catch {
				// 失敗は通知未実装: TODO
			}
		}
	}

	/// <summary>
	/// 設定ファイルパスを取得します。ワークスペース未設定時は AppData 既定パスを返します。
	/// </summary>
	public string GetConfigFilePath(string fileName) {
		if (!string.IsNullOrWhiteSpace(this.WorkspacePath)) {
			return Path.Combine(this.WorkspacePath, fileName);
		}
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RemoteLogViewer", fileName);
	}
}
