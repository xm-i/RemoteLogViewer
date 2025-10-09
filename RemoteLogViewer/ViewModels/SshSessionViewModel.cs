namespace RemoteLogViewer.ViewModels;

/// <summary>
///     SSH 接続設定と接続後の状態管理を行います。
/// </summary>
[AddTransient]
public class SshSessionViewModel {
	private ObservableList<string> _entries { get; } = [];
	/// <summary>
	///     ホスト名。
	/// </summary>
	public BindableReactiveProperty<string> Host { get; } = new("");
	/// <summary>
	///     ポート番号。
	/// </summary>
	public BindableReactiveProperty<int> Port { get; } = new(22);
	/// <summary>
	///     ユーザー名。
	/// </summary>
	public BindableReactiveProperty<string> User { get; } = new("");
	/// <summary>
	///     パスワード。
	/// </summary>
	public BindableReactiveProperty<string> Password { get; } = new("");
	/// <summary>
	///     接続済みかどうか。
	/// </summary>
	public BindableReactiveProperty<bool> IsConnected { get; } = new(false);
	/// <summary>
	///     ルートディレクトリ一覧。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<string> Entries {
		get;
	}

	/// <summary>
	///     接続コマンド。
	/// </summary>
	public ReactiveCommand ConnectCommand { get; } = new();

	private readonly Services.SshService _sshService;

	/// <summary>
	///     <see cref="SshSessionViewModel"/> の新しいインスタンスを初期化します。
	/// </summary>
	public SshSessionViewModel(Services.SshService sshService) {
		this._sshService = sshService;
		this.ConnectCommand.Subscribe(_ => this.Connect());
		this.Entries = this._entries.ToNotifyCollectionChanged();
	}

	/// <summary>
	///     SSH 接続を実施し ls / の結果を取得します。
	/// </summary>
	private void Connect() {
		if (this.IsConnected.Value) {
			return;
		}
		this._sshService.Connect(this.Host.Value, this.Port.Value, this.User.Value, this.Password.Value);
		this.IsConnected.Value = true;
		this._entries.Clear();
		var output = this._sshService.Run("ls /");
		foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
			this._entries.Add(line);
		}
	}
}
