using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>
/// アプリケーション設定全体モデル。カテゴリ毎の設定を保持します。
/// </summary>
[AddScoped]
public class SettingsModel {
	/// <summary>ハイライト設定一覧。</summary>
	public ReactiveProperty<HighlightSettingModel> HighlightSetting { get; } = new();
}

public class SettingsModelForJson {
	public HighlightSettingModelForJson? HighlightSetting {
		get; set;
	}
	public static SettingsModel CreateModel(SettingsModelForJson json, IServiceProvider service) {
		var scope = service.CreateScope();
		var model = scope.ServiceProvider.GetRequiredService<SettingsModel>();
		if (json.HighlightSetting != null) {
			model.HighlightSetting.Value = HighlightSettingModelForJson.CreateModel(json.HighlightSetting, scope.ServiceProvider);
		}
		return model;
	}
	public static SettingsModelForJson CreateJson(SettingsModel model) {
		return new SettingsModelForJson {
			HighlightSetting = HighlightSettingModelForJson.CreateJson(model.HighlightSetting.Value)
		};
	}
}
