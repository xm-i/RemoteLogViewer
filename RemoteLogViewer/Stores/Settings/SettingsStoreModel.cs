using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using RemoteLogViewer.Services;
using RemoteLogViewer.Stores.Settings.Model;

namespace RemoteLogViewer.Stores.Settings;

/// <summary>
///     設定の保存と読み込みを行います。
/// </summary>
[AddSingleton]
public class SettingsStoreModel {
	private readonly WorkspaceService _workspaceService;
	private readonly IServiceProvider _service;
	private string FilePath {
		get {
			return this._workspaceService.GetConfigFilePath("settings.json");
		}
	}
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
	/// <summary>
	///     設定一覧を取得します。
	/// </summary>
	public ReactiveProperty<SettingsModel> SettingsModel {
		get;
	} = new();

	public SettingsStoreModel(WorkspaceService workspaceService, IServiceProvider service) {
		this._workspaceService = workspaceService;
		this._service = service;
		this.Load();
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	public void Load() {
		var scope = this._service.CreateScope();
		try {
			if (File.Exists(this.FilePath)) {
				var json = File.ReadAllText(this.FilePath);
				var loaded = JsonSerializer.Deserialize<SettingsModelForJson>(json);
				if (loaded != null) {
					this.SettingsModel.Value = SettingsModelForJson.CreateModel(loaded, scope.ServiceProvider);
				} else {
					this.SettingsModel.Value = scope.ServiceProvider.GetRequiredService<SettingsModel>();
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
			Directory.CreateDirectory(Path.GetDirectoryName(this.FilePath)!);
			var json = JsonSerializer.Serialize(SettingsModelForJson.CreateJson(this.SettingsModel.Value), _jsonSerializerOptions);
			File.WriteAllText(this.FilePath, json);
		} catch {
			// TODO: 失敗通知
		}
	}
}
