using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>
/// アプリケーション設定全体モデル。カテゴリ毎の設定を保持します。
/// </summary>
[AddSingleton]
public class SettingsModel(HighlightSettingsModel highlightSettingModel, TextViewerSettingsModel textViewerSettingsModel, AdvancedSettingsModel advancedSettingsModel) {
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

	public AdvancedSettingsModel AdvancedSettings {
		get; set;
	} = advancedSettingsModel;
}

public class SettingsModelForJson {
	public HighlightSettingsModelForJson? HighlightSettings {
		get; set;
	}

	public TextViewerSettingsModelForJson? TextViewerSettings {
		get; set;
	}

	public AdvancedSettingsModelForJson? AdvancedSettings {
		get; set;
	}

	public static SettingsModel CreateModel(SettingsModelForJson json, IServiceProvider service) {
		var model = service.GetRequiredService<SettingsModel>();
		if (json.HighlightSettings is { } highlightSettings) {
			model.HighlightSettings = HighlightSettingsModelForJson.CreateModel(highlightSettings, service);
		}
		if (json.TextViewerSettings is { } textViewerSettings) {
			model.TextViewerSettings = TextViewerSettingsModelForJson.CreateModel(textViewerSettings, service);
		}
		if (json.AdvancedSettings is { } advancedSettings) {
			model.AdvancedSettings = AdvancedSettingsModelForJson.CreateModel(advancedSettings, service);
		}
		return model;
	}
	public static SettingsModelForJson CreateJson(SettingsModel model) {
		return new SettingsModelForJson {
			HighlightSettings = HighlightSettingsModelForJson.CreateJson(model.HighlightSettings),
			TextViewerSettings = TextViewerSettingsModelForJson.CreateJson(model.TextViewerSettings),
			AdvancedSettings = AdvancedSettingsModelForJson.CreateJson(model.AdvancedSettings),
		};
	}
}
