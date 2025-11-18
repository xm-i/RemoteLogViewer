using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using ObservableCollections;

using R3.JsonConfig.Attributes;

using RemoteLogViewer.Composition.Utils.Attributes;

namespace RemoteLogViewer.Composition.Stores.Ssh;

/// <summary>
///     SSH 接続設定情報の一覧を保持するモデルを表します。
/// </summary>
[AddSingleton]
[GenerateR3JsonConfigDto]
public class SshConnectionProfileModel(IServiceProvider serviceProvider) {
	/// <summary>
	///     設定一覧を取得します。
	/// </summary>
	public ObservableList<SshConnectionInfoModel> Items {
		get;
	} = [];

	public void Remove(Guid id) {
		var target = this.Items.FirstOrDefault(x => x.Id.Value == id);
		if (target != null) {
			this.Items.Remove(target);
		}
	}

	public void Add() {
		var scope = serviceProvider.CreateScope();
		var scim = scope.ServiceProvider.GetRequiredService<SshConnectionInfoModel>();
		scim.Id.Value = Guid.NewGuid();
		this.Items.Add(scim);
	}
}