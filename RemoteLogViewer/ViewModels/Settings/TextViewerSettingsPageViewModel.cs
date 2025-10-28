using RemoteLogViewer.Stores.Settings;

namespace RemoteLogViewer.ViewModels.Settings;

/// <summary>
/// TextViewer設定ページ用 ViewModel です。
/// </summary>
[AddTransient]
public class TextViewerSettingsPageViewModel : SettingsPageViewModel {
	/// <summary>
	/// 1行に表示する最大文字数
	/// </summary>
	public BindableReactiveProperty<int> MaxPreviewOneLineCharacters {
		get;
	}

	/// <summary>
	/// 全体で表示する最大文字数
	/// </summary>
	public BindableReactiveProperty<int> MaxPreviewCharacters {
		get;
	}

	/// <summary>コンストラクタ。</summary>
	public TextViewerSettingsPageViewModel(SettingsStoreModel settingsStoreModel) : base("TextViewer") {
		this.MaxPreviewOneLineCharacters = settingsStoreModel.SettingsModel.TextViewerSettings.MaxPreviewOneLineCharacters.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.MaxPreviewCharacters = settingsStoreModel.SettingsModel.TextViewerSettings.MaxPreviewCharacters.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
	}
}
