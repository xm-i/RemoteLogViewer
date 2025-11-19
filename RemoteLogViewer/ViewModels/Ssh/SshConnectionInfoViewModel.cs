using RemoteLogViewer.Composition.Stores.Ssh;
using RemoteLogViewer.Stores.SshConnection;

namespace RemoteLogViewer.ViewModels.Ssh;

[Inject(InjectServiceLifetime.Scoped)]
public class SshConnectionInfoViewModel {
	private readonly Subject<Unit> _sshConnectionInfoUpdated = new();
	/// <summary>
	/// モデル
	/// </summary>
	public SshConnectionInfoModel Model {
		get;
	}

	/// <summary>
	/// 接続表示名。
	/// </summary>
	public BindableReactiveProperty<string> Name {
		get;
	}

	/// <summary>
	/// ホスト名。
	/// </summary>
	public BindableReactiveProperty<string> Host {
		get;
	}
	/// <summary>
	/// ポート番号。
	/// </summary>
	public BindableReactiveProperty<int> Port {
		get;
	}
	/// <summary>
	/// ユーザー名。
	/// </summary>
	public BindableReactiveProperty<string> User {
		get;
	}
	/// <summary>
	/// パスワード。
	/// </summary>
	public BindableReactiveProperty<string> Password {
		get;
	}

	/// <summary>
	/// 秘密鍵パス。
	/// </summary>
	public BindableReactiveProperty<string?> PrivateKeyPath {
		get;
	}

	/// <summary>
	/// 秘密鍵パスフレーズ。
	/// </summary>
	public BindableReactiveProperty<string?> PrivateKeyPassphrase {
		get;
	}

	/// <summary>
	/// 文字エンコード名。
	/// </summary>
	public BindableReactiveProperty<string> EncodingString {
		get;
	}

	/// <summary>
	/// 表示用文字列 (Name が空なら user@host:port)。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<string> DisplayName {
		get;
	}

	/// <summary>
	/// 保存コマンド
	/// </summary>
	public ReactiveCommand SaveConnectionInfoCommand {
		get;
	} = new();

	public ReactiveCommand RemoveConnectionInfoCommand {
		get;
	} = new();

	public IReadOnlyBindableReactiveProperty<bool> IsEdited {
		get;
	}

	public SshConnectionInfoViewModel(SshConnectionInfoModel sshConnectionInfoModel, SshConnectionStoreModel sshConnectionStoreModel) {
		this.Model = sshConnectionInfoModel;
		this.Name = this.Model.Name!.ToBindableReactiveProperty()!;
		this.Host = this.Model.Host!.ToBindableReactiveProperty()!;
		this.Port = this.Model.Port.ToBindableReactiveProperty();
		this.User = this.Model.User!.ToBindableReactiveProperty()!;
		this.Password = this.Model.Password!.ToBindableReactiveProperty()!;
		this.PrivateKeyPath = this.Model.PrivateKeyPath.ToBindableReactiveProperty();
		this.PrivateKeyPassphrase = this.Model.PrivateKeyPassphrase.ToBindableReactiveProperty();
		this.EncodingString = this.Model.EncodingString!.ToBindableReactiveProperty()!;
		this.DisplayName = this.Name
			.CombineLatest(this.User, this.Host, this.Port, (n, u, h, p) => string.IsNullOrWhiteSpace(n) ? $"{u}@{h}:{p}" : n)
			.ToReadOnlyBindableReactiveProperty(string.Empty);

		this.IsEdited = this.Name.ToUnit()
			.Merge(this.Host.ToUnit())
			.Merge(this.Port.ToUnit())
			.Merge(this.User.ToUnit())
			.Merge(this.Password.ToUnit())
			.Merge(this.PrivateKeyPath.ToUnit())
			.Merge(this.PrivateKeyPassphrase.ToUnit())
			.Merge(this.EncodingString.ToUnit())
			.Merge(this.SaveConnectionInfoCommand.ToUnit())
			.Merge(this._sshConnectionInfoUpdated)
			.Select(_ => {
				return !(
					(this.Name.Value ?? string.Empty) == (this.Model.Name.Value ?? string.Empty) &&
					(this.Host.Value ?? string.Empty) == (this.Model.Host.Value ?? string.Empty) &&
					this.Port.Value == this.Model.Port.Value &&
					(this.User.Value ?? string.Empty) == (this.Model.User.Value ?? string.Empty) &&
					(this.Password.Value ?? string.Empty) == (this.Model.Password.Value ?? string.Empty) &&
					(this.PrivateKeyPath.Value ?? string.Empty) == (this.Model.PrivateKeyPath.Value ?? string.Empty) &&
					(this.PrivateKeyPassphrase.Value ?? string.Empty) == (this.Model.PrivateKeyPassphrase.Value ?? string.Empty) &&
					(this.EncodingString.Value ?? string.Empty) == (this.Model.EncodingString.Value ?? string.Empty)
				);
			}).ToReadOnlyBindableReactiveProperty(false);

		this.SaveConnectionInfoCommand.Subscribe(_ => {
			this.Model.Name.Value = this.Name.Value;
			this.Model.Host.Value = this.Host.Value;
			this.Model.Port.Value = this.Port.Value;
			this.Model.User.Value = this.User.Value;
			this.Model.Password.Value = this.Password.Value;
			this.Model.PrivateKeyPath.Value = this.PrivateKeyPath.Value;
			this.Model.PrivateKeyPassphrase.Value = this.PrivateKeyPassphrase.Value;
			this.Model.EncodingString.Value = this.EncodingString.Value;
			sshConnectionStoreModel.Save();
			this._sshConnectionInfoUpdated.OnNext(Unit.Default);
		});

		this.RemoveConnectionInfoCommand.Subscribe(_ => {
			sshConnectionStoreModel.Remove(this.Model.Id.Value);
		});
	}
}
