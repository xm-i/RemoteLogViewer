using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using RemoteLogViewer.Models.Ssh;

namespace RemoteLogViewer.ViewModels.Ssh;

[AddScoped]
public class SshConnectionInfoViewModel {
	private static readonly ConcurrentDictionary<SshConnectionInfoModel, SshConnectionInfoViewModel> _createdInstances = [];
	/// <summary>
	/// モデル
	/// </summary>
	public SshConnectionInfoModel Model {
		get;
	}

	/// <summary>
	/// 接続表示名。
	/// </summary>
	public BindableReactiveProperty<string> Name { get; }

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
	public BindableReactiveProperty<string?> PrivateKeyPath { get; }

	/// <summary>
	/// 秘密鍵パスフレーズ。
	/// </summary>
	public BindableReactiveProperty<string?> PrivateKeyPassphrase { get; }

	/// <summary>
	/// 文字エンコード名。
	/// </summary>
	public BindableReactiveProperty<string> EncodingString { get; }

	/// <summary>
	/// 表示用文字列 (Name が空なら user@host:port)。
	/// </summary>
	public BindableReactiveProperty<string> DisplayName {
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
			.ToBindableReactiveProperty(string.Empty);

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
		});

		this.RemoveConnectionInfoCommand.Subscribe(_ => {
			sshConnectionStoreModel.Remove(this.Model.Id.Value);
		});
	}
}
