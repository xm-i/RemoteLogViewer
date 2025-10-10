using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Models.Ssh;

/// <summary>
///     接続設定の保存と読み込みを行います。
/// </summary>
[AddSingleton]
public class SshConnectionStoreModel {
	private readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RemoteLogViewer", "connections.json");
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
	/// <summary>
	///     設定一覧を取得します。
	/// </summary>
	public ObservableList<SshConnectionInfoModel> Items {
		get;
	} = [];

	public SshConnectionStoreModel() {
		this.Load();
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	public void Load() {
		try {
			if (File.Exists(this._filePath)) {
				var json = File.ReadAllText(this._filePath);
				var loaded = JsonSerializer.Deserialize<List<SshConnectionInfoModelForJson>>(json) ?? [];
				this.Items.Clear();
				foreach (var c in loaded) {
					this.Items.Add(SshConnectionInfoModelForJson.CreateSshConnectionInfoModel(c));
				}
			}
		} catch {
			// TODO: 失敗通知
		}
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(this._filePath)!);
			var json = JsonSerializer.Serialize(this.Items.Select(SshConnectionInfoModelForJson.CreateSshConnectionInfoModelForJson), _jsonSerializerOptions);
			File.WriteAllText(this._filePath, json);
		} catch {
			// TODO: 失敗通知
		}
	}

	public void Remove(Guid id) {
		var target = this.Items.FirstOrDefault(x => x.Id.Value == id);
		if (target != null) {
			this.Items.Remove(target);
			this.Save();
		}
	}

	public void Add() {
		var scope = Ioc.Default.CreateScope();
		this.Items.Add(scope.ServiceProvider.GetRequiredService<SshConnectionInfoModel>());
		this.Save();
	}
}
