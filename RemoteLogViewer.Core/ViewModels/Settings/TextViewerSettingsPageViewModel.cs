using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Stores.Settings;
using RemoteLogViewer.Core.Utils.Extensions;

namespace RemoteLogViewer.Core.ViewModels.Settings;

/// <summary>
/// TextViewer設定ページ用 ViewModel です。
/// </summary>
[Inject(InjectServiceLifetime.Transient)]
public class TextViewerSettingsPageViewModel : SettingsPageViewModel<TextViewerSettingsPageViewModel> {
	/// <summary>
	/// 1度に追加読み込みする行数
	/// </summary>
	public BindableReactiveProperty<int> PrefetchLineCount {
		get;
	}

	/// <summary>
	/// 追加読み込みの閾値行数(残りXX行になったら追加読み込みする。)
	/// </summary>
	public BindableReactiveProperty<int> PrefetchThresholdLines {
		get;
	}

	/// <summary>
	/// 画面内に保持する最大行数
	/// </summary>
	public BindableReactiveProperty<int> MaxLogLineLimit {
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
		this.PrefetchLineCount = settingsStoreModel.SettingsModel.TextViewerSettings.PrefetchLineCount.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.PrefetchThresholdLines = settingsStoreModel.SettingsModel.TextViewerSettings.PrefetchThresholdLines.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.MaxLogLineLimit = settingsStoreModel.SettingsModel.TextViewerSettings.MaxLogLineLimit.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.GrepMaxResults = settingsStoreModel.SettingsModel.TextViewerSettings.GrepMaxResults.ToTwoWayBindableReactiveProperty().AddTo(this.CompositeDisposable);
	}
}
