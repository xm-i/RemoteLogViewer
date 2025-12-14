using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Services;
using RemoteLogViewer.Core.Utils;

namespace RemoteLogViewer.Core.ViewModels;

/// <summary>
///     メインウィンドウ用の ViewModel です。タブ一覧およびタブ追加コマンドを公開します。
/// </summary>
[Inject(InjectServiceLifetime.Singleton)]
public class MainWindowViewModel : ViewModelBase<MainWindowViewModel> {
	private readonly ObservableList<LogViewerViewModel> _tabs = [];
	/// <summary>
	///     ログビュータブの一覧を保持します (UI 自動更新用)。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<LogViewerViewModel> Tabs {
		get;
	}

	public BindableReactiveProperty<LogViewerViewModel?> SelectedTab {
		get;
	} = new();

	/// <summary>
	///     新しいタブを追加するリアクティブコマンドです。
	/// </summary>
	public ReactiveCommand AddTabCommand { get; } = new();

	/// <summary>
	///     タブを閉じるコマンドです。
	/// </summary>
	public ReactiveCommand<LogViewerViewModel> CloseTabCommand { get; } = new();

	/// <summary>
	/// ユーザー通知ストリーム。
	/// </summary>
	public Observable<NotificationInfo> Notifications {
		get;
	}
	/// <summary>
	/// アクション付きユーザー通知ストリーム。
	/// </summary>
	public Observable<NotificationInfoWithAction> NotificationWithActions {
		get;
	}

	/// <summary>
	///     <see cref="MainWindowViewModel"/> の新しいインスタンスを初期化します。
	/// </summary>
	public MainWindowViewModel(NotificationService notificationService, ILogger<MainWindowViewModel> logger) : base(logger) {
		this.Tabs = this._tabs.ToNotifyCollectionChanged();
		this.AddTabCommand.Subscribe(_ => this.AddTab());
		this.CloseTabCommand.Subscribe(this.CloseTab);
		this.AddTab();
		this.Notifications = notificationService.Notifications;
		this.NotificationWithActions = notificationService.NotificationWithActions;
	}

	/// <summary>
	///     新しいログビュータブを追加します。
	/// </summary>
	private void AddTab() {
		var vm = Ioc.Default.CreateScope().ServiceProvider.GetRequiredService<LogViewerViewModel>();
		this._tabs.Add(vm);
		this.SelectedTab.Value = vm;
	}

	/// <summary>
	///     指定したタブを閉じます。SSH 接続中なら切断します。
	/// </summary>
	/// <param name="vm">閉じる対象タブ。</param>
	private void CloseTab(LogViewerViewModel vm) {
		if (!this._tabs.Contains(vm)) {
			return;
		}
		vm.Disconnect();
		this._tabs.Remove(vm);
		if (this.SelectedTab.Value == vm) {
			this.SelectedTab.Value = this._tabs.LastOrDefault();
		}
		vm.Dispose();
	}
}
