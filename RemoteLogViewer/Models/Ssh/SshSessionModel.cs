using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Utils.Extensions;
using RemoteLogViewer.Views.Ssh.FileViewer;

namespace RemoteLogViewer.Models.Ssh;

/// <summary>
///     SSH セッションのドメインモデル。接続状態と接続操作を担います。
/// </summary>
[AddScoped]
public class SshSessionModel {
	private readonly SshService _sshService;
	private readonly SshConnectionStoreModel _store;
	private readonly TextFileViewerModel _textFileViewerModel;
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

	public SshSessionModel(SshService sshService, TextFileViewerModel textFileViewerModel, SshConnectionStoreModel store) {
		this._sshService = sshService;
		this._store = store;
		this._textFileViewerModel = textFileViewerModel;
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
		this._textFileViewerModel.CloseFile();
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
		this._textFileViewerModel.CloseFile();
	}

	public void AddSavedConnection() {
		this._store.Add();
	}
}
