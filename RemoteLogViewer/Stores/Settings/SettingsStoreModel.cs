using System.Diagnostics.CodeAnalysis;
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
	public IServiceProvider ScopedService {
		get;
	}
	private string FilePath {
		get {
			return this._workspaceService.GetConfigFilePath("settings.json");
		}
	}
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
	/// <summary>
	///     設定一覧を取得します。
	/// </summary>
	public SettingsModel SettingsModel {
		get;
		private set;
	}

	public SettingsStoreModel(WorkspaceService workspaceService, IServiceProvider service) {
		this._workspaceService = workspaceService;
		this.ScopedService = service;
		this.Load();
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	[MemberNotNull(nameof(SettingsModel))]
	public void Load() {
		var scope = this.ScopedService.CreateScope();
		try {
			if (File.Exists(this.FilePath)) {
				var json = File.ReadAllText(this.FilePath);
				var loaded = JsonSerializer.Deserialize<SettingsModelForJson>(json);
				if (loaded != null) {
					this.SettingsModel = SettingsModelForJson.CreateModel(loaded, scope.ServiceProvider);
					return;
				}
			}
		} catch {
			// TODO: 失敗通知
		}
		this.SettingsModel = scope.ServiceProvider.GetRequiredService<SettingsModel>();
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(this.FilePath)!);
			var json = JsonSerializer.Serialize(SettingsModelForJson.CreateJson(this.SettingsModel), _jsonSerializerOptions);
			File.WriteAllText(this.FilePath, json);
		} catch {
			// TODO: 失敗通知
		}
	}
}
