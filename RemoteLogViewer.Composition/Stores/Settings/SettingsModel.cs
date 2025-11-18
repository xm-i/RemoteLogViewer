using R3.JsonConfig.Attributes;

using RemoteLogViewer.Composition.Utils.Attributes;

namespace RemoteLogViewer.Composition.Stores.Settings;

/// <summary>
/// アプリケーション設定全体モデル。カテゴリ毎の設定を保持します。
/// </summary>
[AddSingleton]
[GenerateR3JsonConfigDto]
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