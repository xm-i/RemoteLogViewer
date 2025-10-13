using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Utils.Extensions;

namespace RemoteLogViewer.Models.Ssh;

/// <summary>
///     SSH セッションのドメインモデル。接続状態と接続操作を担います。
/// </summary>
[AddScoped]
public class SshSessionModel {
	private readonly SshService _sshService;
	private readonly SshConnectionStoreModel _store;

	/// <summary>接続済みか。</summary>
	public ReactiveProperty<bool> IsConnected {
		get;
	} = new(false);

	/// <summary>現在のパス。</summary>
	public ReactiveProperty<string> CurrentPath {
		get;
	} = new("/");

	/// <summary>ルートエントリ一覧。</summary>
	public ObservableList<FileSystemObject> Entries {
		get;
	} = [];

	/// <summary>保存済み接続一覧 (ストアの Items を公開)。</summary>
	public ObservableList<SshConnectionInfoModel> SavedConnections {
		get;
	}

	public ReactiveProperty<SshConnectionInfoModel?> SelectedSshConnectionInfo {
		get;
	} = new(null);

	/// <summary>開いているファイルのフルパス。</summary>
	public ReactiveProperty<string?> OpenedFilePath {
		get;
	} = new(null);

	/// <summary>開いているファイル内容。</summary>
	public ReactiveProperty<string?> FileContent {
		get;
	} = new(null);

	public SshSessionModel(SshService sshService, SshConnectionStoreModel store) {
		this._sshService = sshService;
		this._store = store;
		this.SavedConnections = store.Items;
	}

	/// <summary>
	///     恒久接続 (接続状態を維持) します。
	/// </summary>
	public void Connect() {
		if (this.SelectedSshConnectionInfo.Value is not { } ci) {
			return; //TODO: エラー通知
		}
		this._sshService.Connect(ci.Host.Value, ci.Port.Value, ci.User.Value, ci.Password.Value, ci.PrivateKeyPath.Value, ci.PrivateKeyPassphrase.Value);
		this.NavigateTo("/");
		this.IsConnected.Value = true;
	}

	/// <summary>
	///     指定ディレクトリを読み込みます。
	/// </summary>
	/// <param name="path">パス。</param>
	private void LoadDirectory(string path) {
		var list = this._sshService.ListDirectory(path);
		this.Entries.Clear();
		this.Entries.AddRange(list);
	}

	/// <summary>
	///     絶対パスへ移動します。
	/// </summary>
	/// <param name="path">絶対パス。</param>
	public void NavigateTo(string path) {
		if (string.IsNullOrWhiteSpace(path)) {
			return;
		}
		if (!path.StartsWith('/')) {
			path = this.CurrentPath.Value.TrimEnd('/') + "/" + path.Trim('/');
		}
		path = path.Replace("//", "/");
		if (path.Length > 1 && path.EndsWith('/')) {
			path = path[..^1];
		}
		this.CurrentPath.Value = path;
		this.LoadDirectory(path);
		this.OpenedFilePath.Value = null;
		this.FileContent.Value = null;
	}

	/// <summary>
	///     対象ディレクトリへ移動します。
	/// </summary>
	/// <param name="directoryName">ディレクトリ名</param>
	public void EnterDirectory(string directoryName) {
		if (string.IsNullOrWhiteSpace(directoryName)) {
			return;
		}
		string newPath;
		if (directoryName.StartsWith('/')) {
			newPath = directoryName;
		} else if (directoryName == "..") {
			if (this.CurrentPath.Value != "/") {
				var trimmed = this.CurrentPath.Value.TrimEnd('/');
				var idx = trimmed.LastIndexOf('/');
				newPath = idx <= 0 ? "/" : trimmed[..idx];
			} else {
				newPath = "/";
			}
		} else if (directoryName == ".") {
			newPath = this.CurrentPath.Value;
		} else {
			if (this.CurrentPath.Value == "/") {
				newPath = "/" + directoryName.Trim('/');
			} else {
				newPath = this.CurrentPath.Value.TrimEnd('/') + "/" + directoryName.Trim('/');
			}
		}
		this.NavigateTo(newPath);
	}

	/// <summary>
	///     ファイルを開き内容を取得します。
	/// </summary>
	/// <param name="fso">ファイル。</param>
	public void OpenFile(FileSystemObject fso) {
		if (fso.FileSystemObjectType is not (FileSystemObjectType.File or FileSystemObjectType.Symlink)) {
			return;
		}
		var fullPath = this.CurrentPath.Value == "/" ? "/" + fso.FileName : this.CurrentPath.Value.TrimEnd('/') + "/" + fso.FileName;
		var escaped = fullPath.Replace("\"", "\\\"");
		try {
			var content = this._sshService.Run($"cat \"{escaped}\"");
			this.OpenedFilePath.Value = fullPath;
			this.FileContent.Value = content;
		} catch (Exception ex) {
			this.OpenedFilePath.Value = fullPath;
			this.FileContent.Value = $"[Failed to open file: {ex.Message}]";
		}
	}

	/// <summary>
	///     テスト接続 (状態は保持しない) を行います。
	/// </summary>
	public void TestConnect() {
		if (this.SelectedSshConnectionInfo.Value is not { } ci) {
			return; //TODO: エラー通知
		}
		this._sshService.Connect(ci.Host.Value, ci.Port.Value, ci.User.Value, ci.Password.Value, ci.PrivateKeyPath.Value, ci.PrivateKeyPassphrase.Value);
		this.Disconnect();
	}

	public void Disconnect() {
		this._sshService.Disconnect();
		this.IsConnected.Value = false;
		this.Entries.Clear();
		this.CurrentPath.Value = "/";
		this.OpenedFilePath.Value = null;
		this.FileContent.Value = null;
	}

	public void AddSavedConnection() {
		this._store.Add();
	}
}
