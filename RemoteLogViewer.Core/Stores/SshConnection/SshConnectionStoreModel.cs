using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RemoteLogViewer.Composition.Stores.Ssh;
using RemoteLogViewer.Core.Services;
using RemoteLogViewer.Core.Stores.SerializerContext;

namespace RemoteLogViewer.Core.Stores.SshConnection;

/// <summary>
///     接続設定の保存と読み込みを行います。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class SshConnectionStoreModel {
	private readonly ILogger _logger;
	private readonly WorkspaceService _workspaceService;
	private readonly IServiceProvider _serviceProvider;
	private string FilePath {
		get {
			return this._workspaceService.GetConfigFilePath("connections.json");
		}
	}

	/// <summary>
	///     設定一覧を取得します。
	/// </summary>
	public SshConnectionProfileModel Profile {
		get;
		private set;
	}

	public SshConnectionStoreModel(WorkspaceService workspaceService, IServiceProvider serviceProvider, ILogger<SshConnectionStoreModel> logger) {
		this._logger = logger;
		this._workspaceService = workspaceService;
		this._serviceProvider = serviceProvider;
		this.Load();
	}

	/// <summary>
	///     保存済み設定を読み込みます。
	/// </summary>
	[MemberNotNull(nameof(Profile))]
	public void Load() {
		try {
			if (File.Exists(this.FilePath)) {
				var json = File.ReadAllText(this.FilePath);
				var loaded = JsonSerializer.Deserialize(json, SshConnectionJsonSerializerContext.Default.SshConnectionProfileModelForJson);

				if (loaded != null) {
					this.Profile = SshConnectionProfileModelForJson.CreateModel(loaded, this._serviceProvider);
					return;
				}
			}
		} catch (Exception ex) {
			// TODO: 失敗通知
			this._logger.LogWarning(ex, "Failed to load connections settings from {FilePath}", this.FilePath);
		}

		this.Profile = this._serviceProvider.GetRequiredService<SshConnectionProfileModel>();
	}

	/// <summary>
	///     現在の設定をファイルへ保存します。
	/// </summary>
	public void Save() {
		try {
			Directory.CreateDirectory(Path.GetDirectoryName(this.FilePath)!);
			var json = JsonSerializer.Serialize(SshConnectionProfileModelForJson.CreateJson(this.Profile), SshConnectionJsonSerializerContext.Default.SshConnectionProfileModelForJson);
			File.WriteAllText(this.FilePath, json);
		} catch (Exception ex) {
			// TODO: 失敗通知
			this._logger.LogWarning(ex, "Failed to save connections settings to {FilePath}", this.FilePath);
		}
	}

	public void Remove(Guid id) {
		this.Profile.Remove(id);
		this.Save();
	}

	public void Add() {
		this.Profile.Add();
		this.Save();
	}
}
