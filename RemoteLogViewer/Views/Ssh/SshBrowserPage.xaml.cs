using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;

using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.ViewModels.Ssh;

namespace RemoteLogViewer.Views.SshSession;

/// <summary>
/// SSH ブラウザページです。リモートファイルシステムの閲覧およびファイルオープン操作を提供します。
/// </summary>
public sealed partial class SshBrowserPage : Page {
	/// <summary>
	/// 閲覧用 ViewModel を取得します。
	/// </summary>
	public SshBrowserViewModel? ViewModel {
		get;
		private set;
	}

	/// <summary>
	/// インスタンスを初期化します。
	/// </summary>
	public SshBrowserPage() {
		this.InitializeComponent();
	}

	/// <summary>
	/// ナビゲート時に ViewModel を受け取ります。
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		if (e.Parameter is SshBrowserViewModel vm) {
			this.ViewModel = vm;
		} else {
			throw new InvalidOperationException("ViewModel is not passed.");
		}
		base.OnNavigatedTo(e);
	}

	/// <summary>
	/// ディレクトリエントリをダブルタップした際に開く処理を実行します。
	/// </summary>
	private void Entries_DoubleTapped(object sender, DoubleTappedRoutedEventArgs _) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is ListView lv && lv.SelectedItem is FileSystemObject fso) {
			this.ViewModel.OpenCommand.Execute(fso);
		}
	}

	/// <summary>
	/// パス入力テキストボックスで Enter キー押下時にナビゲーションを実行します。
	/// </summary>
	private void CurrentPathTextBox_KeyDown(object _, KeyRoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (e.Key == Windows.System.VirtualKey.Enter) {
			this.ViewModel.NavigatePathCommand.Execute(Unit.Default);
			e.Handled = true;
		}
	}
}
