using RemoteLogViewer.Services;
using RemoteLogViewer.Utils.Extensions;

namespace RemoteLogViewer.Models.Ssh;

/// <summary>
///     SSH セッションのドメインモデル。接続状態と接続操作を担います。
/// </summary>
[AddTransient]
public class SshSessionModel {
	private readonly SshService _sshService;
	private readonly SshConnectionStoreModel _store;

	/// <summary>接続済みか。</summary>
	public ReactiveProperty<bool> IsConnected {
		get;
	} = new(false);

	/// <summary>ルートエントリ一覧。</summary>
	public ObservableList<string> Entries {
		get;
	} = [];

	/// <summary>保存済み接続一覧 (ストアの Items を公開)。</summary>
	public ObservableList<SshConnectionInfoModel> SavedConnections {
		get;
	}

	public ReactiveProperty<SshConnectionInfoModel?> SelectedSshConnectionInfo {
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
		this.Entries.Clear();
		var output = this._sshService.ListDirectory("/");
		this.Entries.AddRange(output);
		this.IsConnected.Value = true;
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
	}

	public void AddSavedConnection() {
		this._store.Add();
	}
}
