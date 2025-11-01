using Microsoft.Extensions.Logging;

using RemoteLogViewer.Stores.Settings;

namespace RemoteLogViewer.ViewModels.Settings;

/// <summary>
/// TextViewer設定ページ用 ViewModel です。
/// </summary>
[AddTransient]
public class TextViewerSettingsPageViewModel : SettingsPageViewModel<TextViewerSettingsPageViewModel> {
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

	/// <summary>
	/// Grep の最大件数
	/// </summary>
	public BindableReactiveProperty<int> GrepMaxResults {
		get;
	}

	/// <summary>コンストラクタ。</summary>
	public TextViewerSettingsPageViewModel(SettingsStoreModel settingsStoreModel, ILogger<TextViewerSettingsPageViewModel> logger) : base("TextViewer", logger) {
		this.MaxPreviewOneLineCharacters = settingsStoreModel.SettingsModel.TextViewerSettings.MaxPreviewOneLineCharacters.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.MaxPreviewCharacters = settingsStoreModel.SettingsModel.TextViewerSettings.MaxPreviewCharacters.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.GrepMaxResults = settingsStoreModel.SettingsModel.TextViewerSettings.GrepMaxResults.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
	}
}
