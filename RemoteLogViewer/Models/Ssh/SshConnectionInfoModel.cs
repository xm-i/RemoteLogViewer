using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Models.Ssh;

/// <summary>
///     SSH 接続設定情報を表します。
/// </summary>
[AddScoped]
public class SshConnectionInfoModel {
	/// <summary>ID。</summary>
	public ReactiveProperty<Guid> Id { get; } = new(Guid.Empty);
	/// <summary>ホスト。</summary>
	public ReactiveProperty<string> Host { get; } = new(string.Empty);
	/// <summary>ポート。</summary>
	public ReactiveProperty<int> Port { get; } = new(22);
	/// <summary>ユーザー。</summary>
	public ReactiveProperty<string> User { get; } = new(string.Empty);
	/// <summary>パスワード。</summary>
	public ReactiveProperty<string> Password { get; } = new(string.Empty);
	public IServiceProvider ServiceProvider {
		get;
	}

	public SshConnectionInfoModel(IServiceProvider serviceProvider) {
		this.ServiceProvider = serviceProvider;
	}
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

	public required string Password {
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
		return model;
	}

	public static SshConnectionInfoModelForJson CreateSshConnectionInfoModelForJson(SshConnectionInfoModel model) {
		return new SshConnectionInfoModelForJson {
			Id = model.Id.Value.ToString(),
			Host = model.Host.Value,
			Port = model.Port.Value,
			User = model.User.Value,
			Password = model.Password.Value,
		};
	}
}