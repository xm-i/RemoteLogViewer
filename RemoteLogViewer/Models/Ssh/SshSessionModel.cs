using System.Text;

using Microsoft.Extensions.Logging;

using RemoteLogViewer.Composition.Stores.Ssh;
using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.Services;
using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Stores.SshConnection;

namespace RemoteLogViewer.Models.Ssh;

/// <summary>
///     SSH セッションのドメインモデル。接続状態と接続操作を担います。
/// </summary>
[AddScoped]
public class SshSessionModel : ModelBase<SshSessionModel> {
	private readonly ISshService _sshService;
	private readonly SshConnectionStoreModel _store;
	private readonly TextFileViewerModel _textFileViewerModel;
	private readonly NotificationService _notificationService;
	/// <summary>接続済みか。</summary>
	public ReactiveProperty<bool> IsConnected {
		get;
	} = new(false);

	public ReactiveProperty<bool> DisconnectedWithException {
		get;
	} = new(false);

	/// <summary>現在のパス。</summary>
	public ReactiveProperty<string> CurrentPath {
		get;
	} = new("/");

	/// <summary>
	/// 現在のパスがブックマークされているか。
	/// </summary>
	public ReadOnlyReactiveProperty<bool> IsCurrentDirectoryBookmarked {
		get;
	}

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

	public EncodingInfo[] AvailableEncodings {
		get;
	} = Encoding.GetEncodings().Where(x => Constants.EncodingPairs.Any(ep => ep.CSharp == x.Name)).ToArray();

	public SshSessionModel(ISshService sshService, TextFileViewerModel textFileViewerModel, SshConnectionStoreModel store, NotificationService notificationService, ILogger<SshSessionModel> logger) : base(logger) {
		this._sshService = sshService.AddTo(this.CompositeDisposable);
		this._store = store;
		this._textFileViewerModel = textFileViewerModel.AddTo(this.CompositeDisposable);
		this._notificationService = notificationService;
		this.SavedConnections = store.Profile.Items;
		this.SelectedSshConnectionInfo.Value = this.SavedConnections.FirstOrDefault();
		this.IsCurrentDirectoryBookmarked = this.CurrentPath.Select(x => {
			if (this.SelectedSshConnectionInfo.Value is not { } ci) {
				return false;
			}
			return ci.Bookmarks.Any(b => b.Path.Value == x);
		}).ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);

		this._sshService.DisconnectedWithExceptionNotification.ObserveOnCurrentSynchronizationContext().Subscribe(x => {
			this.DisconnectedWithException.Value = true;
			this._notificationService.Publish("SSH", $"接続が切断されました：{x.Message}", NotificationSeverity.Error, x);
		}).AddTo(this.CompositeDisposable);

		this.SavedConnections.ObserveRemove().Subscribe(x => {
			if (x.Value == this.SelectedSshConnectionInfo.Value) {
				this.SelectedSshConnectionInfo.Value = this.SavedConnections.FirstOrDefault();
			}
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	///     恒久接続 (接続状態を維持) します。
	/// </summary>
	public void Connect(bool isReconnect = false) {
		if (this.SelectedSshConnectionInfo.Value is not { } ci) {
			return; //TODO: エラー通知
		}
		try {
			this._sshService.Connect(ci.Host.Value, ci.Port.Value, ci.User.Value, ci.Password.Value, ci.PrivateKeyPath.Value, ci.PrivateKeyPassphrase.Value, ci.EncodingString.Value);
		} catch (Exception ex) {
			this._notificationService.Publish("SSH", $"接続に失敗しました: {ex.Message}", NotificationSeverity.Error, "再接続", () => this.Connect(isReconnect), "閉じる", () => { }, ex);
			return;
		}
		this.IsConnected.Value = true;
		this.DisconnectedWithException.Value = false;
		if (!isReconnect) {
			this.NavigateTo("/");
			this._textFileViewerModel.LoadAvailableEncoding();
		}
	}

	public void Reconnect() {
		this.Connect(true);
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
		if (directoryName == "..") {
			if (this.CurrentPath.Value != "/") {
				var trimmed = this.CurrentPath.Value.TrimEnd('/');
				var idx = trimmed.LastIndexOf('/') + 1;
				newPath = idx <= 0 ? "/" : trimmed[..idx];
			} else {
				newPath = "/";
			}
		} else if (directoryName == ".") {
			newPath = this.CurrentPath.Value;
		} else {
			newPath = PathUtils.CombineUnixPath(this.CurrentPath.Value, directoryName, FileSystemObjectType.Directory);
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
		this._sshService.Connect(ci.Host.Value, ci.Port.Value, ci.User.Value, ci.Password.Value, ci.PrivateKeyPath.Value, ci.PrivateKeyPassphrase.Value, ci.EncodingString.Value);
		this.Disconnect();
		this._notificationService.Publish("SSH", "接続に成功しました。", NotificationSeverity.Info);
	}

	public void Disconnect() {
		this._sshService.Disconnect();
		this.IsConnected.Value = false;
		this.DisconnectedWithException.Value = false;
	}

	public void AddSavedConnection() {
		this._store.Add();
	}

	/// <summary>
	/// 指定エントリをブックマークへ追加します。
	/// </summary>
	/// <param name="fso">対象エントリ。</param>
	public void AddBookmark(FileSystemObject fso) {
		var targetPath = PathUtils.CombineUnixPath(this.CurrentPath.Value, fso.FileName, fso.FileSystemObjectType);
		this.AddBookmark(targetPath, fso.FileName);
	}

	/// <summary>
	/// 指定エントリをブックマークへ追加します。
	/// </summary>
	/// <param name="absolutePath">絶対パス。</param>
	/// <param name="name">表示名。</param>
	public void AddBookmark(string absolutePath, string name) {
		if (this.SelectedSshConnectionInfo.Value is not { } ci) {
			return;
		}

		var isExists = ci.Bookmarks.Any(b => string.Equals(b.Path.Value, absolutePath, StringComparison.Ordinal));

		if (isExists) {
			return;
		}

		var list = ci.Bookmarks;
		int order;
		if (list.Count == 0) {
			order = 1;
		} else {
			order = list.Max(b => b.Order.Value) + 1;
		}
		var bm = new SshBookmarkModel();
		bm.Order.Value = order;
		bm.Path.Value = absolutePath;
		bm.Name.Value = name;
		list.Add(bm);
		this._store.Save();
	}


	/// <summary>
	/// ブックマークを削除します。
	/// </summary>
	/// <param name="fso">対象エントリ。</param>
	public void RemoveBookmark(FileSystemObject fso) {
		var targetPath = PathUtils.CombineUnixPath(this.CurrentPath.Value, fso.FileName, fso.FileSystemObjectType);
		this.RemoveBookmark(targetPath);
	}
	/// <summary>
	/// ブックマークを削除します。
	/// </summary>
	/// <param name="absolutePath">絶対パス。</param>
	public void RemoveBookmark(string absolutePath) {
		if (this.SelectedSshConnectionInfo.Value is not { } ci) {
			return;
		}

		var existing = ci.Bookmarks.FirstOrDefault(b => string.Equals(b.Path.Value, absolutePath, StringComparison.Ordinal));

		if (existing == null) {
			return;
		}
		ci.Bookmarks.Remove(existing);
		this._store.Save();
	}
}
