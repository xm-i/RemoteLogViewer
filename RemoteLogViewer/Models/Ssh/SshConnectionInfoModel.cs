using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Models.Ssh;

/// <summary>
///     SSH 接続設定情報を表します。
/// </summary>
[AddScoped]
public class SshConnectionInfoModel {
	public ReactiveProperty<string> Name {
		get;
	} = new(string.Empty);
	/// <summary>ID。</summary>
	public ReactiveProperty<Guid> Id { get; } = new(Guid.Empty);
	/// <summary>ホスト。</summary>
	public ReactiveProperty<string> Host { get; } = new(string.Empty);
	/// <summary>ポート。</summary>
	public ReactiveProperty<int> Port { get; } = new(22);
	/// <summary>ユーザー。</summary>
	public ReactiveProperty<string> User { get; } = new(string.Empty);
	/// <summary>パスワード。</summary>
	public ReactiveProperty<string?> Password { get; } = new(null);
	/// <summary>秘密鍵パス。</summary>
	public ReactiveProperty<string?> PrivateKeyPath { get; } = new(null);
	/// <summary>秘密鍵パスフレーズ。</summary>
	public ReactiveProperty<string?> PrivateKeyPassphrase { get; } = new(null);
	public IServiceProvider ServiceProvider {
		get;
	}

	public SshConnectionInfoModel(IServiceProvider serviceProvider) {
		this.ServiceProvider = serviceProvider;
	}

	public ReactiveProperty<string> EncodingString {
		get;
	} = new("UTF-8");
}

public class SshConnectionInfoModelForJson {
	public required string Id {
		get;
		init;
	}

	public required string Host {
		get;
		init;
	}

	public required int Port {
		get;
		init;
	}

	public required string User {
		get;
		init;
	}

	public string? Password {
		get;
		init;
	}
	public string? PrivateKeyPath {
		get;
		init;
	}
	public string? PrivateKeyPassphrase {
		get;
		init;
	}
	public required string Name {
		get;
		init;
	}
	public required string EncodingString {
		get;
		init;
	}

	public static SshConnectionInfoModel CreateSshConnectionInfoModel(SshConnectionInfoModelForJson json) {
		var scope = Ioc.Default.CreateScope();
		var model = scope.ServiceProvider.GetRequiredService<SshConnectionInfoModel>();
		model.Id.Value = Guid.Parse(json.Id);
		model.Host.Value = json.Host;
		model.Port.Value = json.Port;
		model.User.Value = json.User;
		model.Password.Value = json.Password;
		model.PrivateKeyPath.Value = json.PrivateKeyPath;
		model.PrivateKeyPassphrase.Value = json.PrivateKeyPassphrase;
		model.Name.Value = json.Name;
		model.EncodingString.Value = json.EncodingString;
		return model;
	}

	public static SshConnectionInfoModelForJson CreateSshConnectionInfoModelForJson(SshConnectionInfoModel model) {
		return new SshConnectionInfoModelForJson {
			Id = model.Id.Value.ToString(),
			Host = model.Host.Value,
			Port = model.Port.Value,
			User = model.User.Value,
			Password = string.IsNullOrWhiteSpace(model.Password.Value) ? null : model.Password.Value,
			PrivateKeyPath = string.IsNullOrWhiteSpace(model.PrivateKeyPath.Value) ? null : model.PrivateKeyPath.Value,
			PrivateKeyPassphrase = string.IsNullOrWhiteSpace(model.PrivateKeyPassphrase.Value) ? null : model.PrivateKeyPassphrase.Value,
			Name = model.Name.Value,
			EncodingString = model.EncodingString.Value,
		};
	}
}