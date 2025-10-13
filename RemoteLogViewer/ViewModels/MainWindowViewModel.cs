using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.ViewModels;

/// <summary>
///     メインウィンドウ用の ViewModel です。タブ一覧およびタブ追加コマンドを公開します。
/// </summary>
[AddSingleton]
public class MainWindowViewModel {
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
	///     <see cref="MainWindowViewModel"/> の新しいインスタンスを初期化します。
	/// </summary>
	public MainWindowViewModel() {
		this.Tabs = this._tabs.ToNotifyCollectionChanged();
		this.AddTabCommand.Subscribe(_ => this.AddTab());
	}

	/// <summary>
	///     新しいログビュータブを追加します。
	/// </summary>
	private void AddTab() {
		var vm = Ioc.Default.CreateScope().ServiceProvider.GetRequiredService<LogViewerViewModel>();
		vm.Title = $"Log {this.Tabs.Count + 1}";
		this._tabs.Add(vm);
		this.SelectedTab.Value = vm;
	}
}
