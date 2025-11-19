using System;

using ObservableCollections;

using R3;
using R3.JsonConfig.Attributes;

namespace RemoteLogViewer.Composition.Stores.Ssh;

/// <summary>
///     SSH 接続設定情報を表します。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
[GenerateR3JsonConfigDto]
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

	/// <summary>ブックマーク一覧。</summary>
	public ObservableList<SshBookmarkModel> Bookmarks { get; } = [];

	public SshConnectionInfoModel(IServiceProvider serviceProvider) {
		this.ServiceProvider = serviceProvider;
	}

	public ReactiveProperty<string> EncodingString {
		get;
	} = new("UTF-8");
}