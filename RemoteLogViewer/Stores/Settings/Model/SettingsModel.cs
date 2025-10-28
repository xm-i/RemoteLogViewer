using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>
/// アプリケーション設定全体モデル。カテゴリ毎の設定を保持します。
/// </summary>
[AddSingleton]
public class SettingsModel(HighlightSettingsModel highlightSettingModel, TextViewerSettingsModel textViewerSettingsModel) {
	/// <summary>ハイライト設定。</summary>
	public HighlightSettingsModel HighlightSettings {
		get;
		set;
	} = highlightSettingModel;
	/// <summary>TextViewer設定。</summary>
	public TextViewerSettingsModel TextViewerSettings {
		get;
		set;
	} = textViewerSettingsModel;
}

public class SettingsModelForJson {
	public HighlightSettingsModelForJson? HighlightSettings {
		get; set;
	}

	public TextViewerSettingsModelForJson? TextViewerSettings {
		get; set;
	}

	public static SettingsModel CreateModel(SettingsModelForJson json, IServiceProvider service) {
		var model = service.GetRequiredService<SettingsModel>();
		if (json.HighlightSettings != null) {
			model.HighlightSettings = HighlightSettingsModelForJson.CreateModel(json.HighlightSettings, service);
		}
		if (json.TextViewerSettings != null) {
			model.TextViewerSettings = TextViewerSettingsModelForJson.CreateModel(json.TextViewerSettings, service);
		}
		return model;
	}
	public static SettingsModelForJson CreateJson(SettingsModel model) {
		return new SettingsModelForJson {
			HighlightSettings = HighlightSettingsModelForJson.CreateJson(model.HighlightSettings),
			TextViewerSettings = TextViewerSettingsModelForJson.CreateJson(model.TextViewerSettings)

		};
	}
}
