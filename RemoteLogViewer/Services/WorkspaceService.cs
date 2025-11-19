using System.IO;
using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.Services;

/// <summary>
/// ワークスペース(設定ファイル保存先)の管理サービス。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class WorkspaceService {
	private readonly ILogger<WorkspaceService> _logger;
	private readonly string _persistFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RemoteLogViewer", "WorkSpacePath.txt");

	/// <summary>
	/// ワークスペース設定を保存するか否かの設定
	/// </summary>
	public bool IsPersist {
		get;
		private set;
	} = false;

	/// <summary>選択済みワークスペースパス。未選択の場合は null。</summary>
	public string? WorkspacePath {
		get; private set;
	}

	public WorkspaceService(ILogger<WorkspaceService> logger) {
		this._logger = logger;
		try {
			if (File.Exists(this._persistFilePath)) {
				var path = File.ReadAllText(this._persistFilePath).Trim();
				if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) {
					this.WorkspacePath = path;
					this.IsPersist = true;
				}
			}
		} catch (Exception ex) {
			this._logger.LogWarning(ex, "Failed to load workspace settings");
		}
	}

	/// <summary>
	/// ワークスペースを設定します。
	/// </summary>
	/// <param name="path">ディレクトリパス。</param>
	/// <param name="persist">次回以降確認を省略する (保存する) 場合 true。</param>
	public void SetWorkspace(string path, bool persist) {
		this.WorkspacePath = path;
		this.IsPersist = persist;
		if (persist) {
			try {
				Directory.CreateDirectory(Path.GetDirectoryName(this._persistFilePath)!);
				File.WriteAllText(this._persistFilePath, path);
			} catch (Exception ex) {
				this._logger.LogWarning(ex, "Failed to save workspace settings");
			}
		} else {
			if (File.Exists(this._persistFilePath)) {
				try {
					File.Delete(this._persistFilePath);
				} catch (Exception ex) {
					this._logger.LogWarning(ex, "Failed to delete workspace settings file");
				}
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
