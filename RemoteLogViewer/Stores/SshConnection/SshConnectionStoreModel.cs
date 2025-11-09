using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RemoteLogViewer.Models.Ssh;
using RemoteLogViewer.Services;

namespace RemoteLogViewer.Stores.SshConnection;

/// <summary>
///     接続設定の保存と読み込みを行います。
/// </summary>
[AddSingleton]
public class SshConnectionStoreModel {
	private readonly ILogger _logger;
	private readonly WorkspaceService _workspaceService;
	private readonly IServiceProvider _serviceProvider;
	private string FilePath {
		get {
			return this._workspaceService.GetConfigFilePath("connections.json");
		}
	}
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
	/// <summary>
	///     設定一覧を取得します。
	/// </summary>
	public ObservableList<SshConnectionInfoModel> Items {
		get;
	} = [];

	public SshConnectionStoreModel(WorkspaceService workspaceService, IServiceProvider serviceProvider,ILogger<SshConnectionStoreModel> logger) {
		this._logger = logger;
		this._workspaceService = workspaceService;
		this._serviceProvider = serviceProvider;
		this.Load();
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	public void Load() {
		try {
			if (File.Exists(this.FilePath)) {
				var json = File.ReadAllText(this.FilePath);
				var loaded = JsonSerializer.Deserialize<List<SshConnectionInfoModelForJson>>(json) ?? [];
				this.Items.Clear();
				foreach (var c in loaded) {
					this.Items.Add(SshConnectionInfoModelForJson.CreateModel(c, this._serviceProvider.CreateScope().ServiceProvider));
				}
			}
		} catch(Exception ex) {
			// TODO: 失敗通知
			this._logger.LogWarning(ex, "Failed to load connections settings from {FilePath}", this.FilePath);
		}
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(this.FilePath)!);
			var json = JsonSerializer.Serialize(this.Items.Select(SshConnectionInfoModelForJson.CreateJson), _jsonSerializerOptions);
			File.WriteAllText(this.FilePath, json);
		} catch (Exception ex) {
			// TODO: 失敗通知
			this._logger.LogWarning(ex, "Failed to save connections settings to {FilePath}", this.FilePath);
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
		var scope = this._serviceProvider.CreateScope();
		var scim = scope.ServiceProvider.GetRequiredService<SshConnectionInfoModel>();
		scim.Id.Value = Guid.NewGuid();
		this.Items.Add(scim);
		this.Save();
	}
}
