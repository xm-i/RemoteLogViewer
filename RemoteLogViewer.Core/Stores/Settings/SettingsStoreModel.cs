using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteLogViewer.Composition.Stores.Settings;
using RemoteLogViewer.Core.Services;
using RemoteLogViewer.Core.Stores.SerializerContext;

namespace RemoteLogViewer.Core.Stores.Settings;

/// <summary>
///     設定の保存と読み込みを行います。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class SettingsStoreModel {
	private readonly ILogger _logger;
	private readonly WorkspaceService _workspaceService;
	private readonly Subject<Unit> _settingsUpdatedSubject = new();
	
	public IServiceProvider ScopedService {
		get;
	}
	private string FilePath {
		get {
			return this._workspaceService.GetConfigFilePath("settings.json");
		}
	}

	public Observable<Unit> SettingsUpdated {
		get {
			return field ??= this._settingsUpdatedSubject.AsObservable();
		}
	}

	/// <summary>
	///     設定一覧を取得します。
	/// </summary>
	public SettingsModel SettingsModel {
		get;
		private set;
	}

	public SettingsStoreModel(WorkspaceService workspaceService, IServiceProvider service, ILogger<SettingsStoreModel> logger) {
		this._logger = logger;
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
				var loaded = JsonSerializer.Deserialize(json, SettingsJsonSerializerContext.Default.SettingsModelForJson);
				if (loaded != null) {
					this.SettingsModel = SettingsModelForJson.CreateModel(loaded, scope.ServiceProvider);
					return;
				}
			}
		} catch (Exception ex) {
			// TODO: 失敗通知
			this._logger.LogWarning(ex, "Failed to load settings from {FilePath}", this.FilePath);
		}
		this.SettingsModel = scope.ServiceProvider.GetRequiredService<SettingsModel>();
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(this.FilePath)!);
			var json = JsonSerializer.Serialize(SettingsModelForJson.CreateJson(this.SettingsModel), SettingsJsonSerializerContext.Default.SettingsModelForJson);
			File.WriteAllText(this.FilePath, json);
			this._settingsUpdatedSubject.OnNext(Unit.Default);
		} catch (Exception ex) {
			// TODO: 失敗通知
			this._logger.LogWarning(ex, "Failed to save settings to {FilePath}", this.FilePath);
		}
	}
}
