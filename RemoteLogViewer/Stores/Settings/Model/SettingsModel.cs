namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>
/// アプリケーション設定全体モデル。カテゴリ毎の設定を保持します。
/// </summary>
[AddSingleton]
[GenerateSettingsJsonDto]
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