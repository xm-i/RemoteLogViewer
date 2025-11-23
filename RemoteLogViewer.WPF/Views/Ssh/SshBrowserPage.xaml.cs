using System.Windows.Controls;
using System.Windows.Input;

using RemoteLogViewer.Composition.Stores.Ssh;
using RemoteLogViewer.Core.ViewModels.Ssh;

namespace RemoteLogViewer.WPF.Views.Ssh;

/// <summary>
/// SSH ブラウザページです。リモートファイルシステムの閲覧およびファイルオープン操作を提供します。
/// </summary>
public sealed partial class SshBrowserPage : Page {
	/// <summary>
	/// 閲覧用 ViewModel を取得します。
	/// </summary>
	public SshBrowserViewModel? ViewModel {
		get;
		set {
			field = value;
			this.DataContext = field;
		}
	}

	/// <summary>
	/// インスタンスを初期化します。
	/// </summary>
	public SshBrowserPage() {
		this.InitializeComponent();
	}

	/// <summary>
	/// ディレクトリエントリをダブルタップした際に開く処理を実行します。
	/// </summary>
	private void Entries_MouseDoubleClick(object sender, MouseButtonEventArgs _) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is ListView lv && lv.SelectedItem is FileSystemEntryViewModel vm) {
			this.ViewModel.OpenCommand.Execute(vm);
		}
	}

	/// <summary>
	/// パス入力テキストボックスで Enter キー押下時にナビゲーションを実行します。
	/// </summary>
	private void CurrentPathTextBox_KeyDown(object _, KeyEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (e.Key == Key.Enter) {
			this.ViewModel.NavigatePathCommand.Execute(Unit.Default);
			e.Handled = true;
		}
	}

	/// <summary>
	/// ブックマークリストダブルタップでそのパスへ遷移します。
	/// </summary>
	private void Bookmarks_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is ListView lv && lv.SelectedItem is SshBookmarkModel bm) {
			this.ViewModel.OpenBookmarkCommand.Execute(bm);
		}
	}

	private void CurrentDirectoryBookmarkedToggleButton_CheckedChange(object sender, System.Windows.RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (this.CurrentDirectoryBookmarkedToggleButton.IsChecked is not { } isChecked) {
			return;
		}
		this.ViewModel.ToggleCurrentDirectoryBookmarkCommand.Execute(isChecked);
	}
}
