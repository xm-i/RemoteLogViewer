using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using RemoteLogViewer.ViewModels.Ssh.FileViewer;
using System.Threading;

namespace RemoteLogViewer.Views.Ssh.FileViewer;

/// <summary>
///     テキストファイルビューア。スクロール位置に応じて行を部分的に読み込みます。
/// </summary>
public sealed partial class TextFileViewer {
	public TextFileViewerViewModel? ViewModel {
		get;
		set {
			field = value;
			if (field == null) {
				return;
			}
			field.WindowStartLine.Subscribe(x => {
				this.VirtualScrollViewer.ScrollToVerticalOffset(x * LineHeight);
			});
		}
	}
	private const long LineHeight = 16;
	public TextFileViewer() {
		this.InitializeComponent();
	}

	private void ContentViewer_SizeChanged(object sender, SizeChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		Debug.WriteLine($"ContentViewer_SizeChanged: {e.NewSize.Height}");
		var visibleLines = Math.Max(1, (int)Math.Floor(e.NewSize.Height / LineHeight) - 1);
		this.ViewModel.VisibleLineCount.Value = visibleLines;
	}

	private void VirtualScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (e.IsIntermediate) {
			// スクロール中は無視
			return;
		}
		Debug.WriteLine($"VirtualScrollViewer_ViewChanged: {this.VirtualScrollViewer.VerticalOffset}");
		this.ViewModel.JumpToLineCommand.Execute((long)this.VirtualScrollViewer.VerticalOffset / LineHeight);
	}

	private void ContentViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		var properties = e.GetCurrentPoint(this.ContentViewer).Properties;
		if (properties.IsHorizontalMouseWheel) {
			return;
		}
		Debug.WriteLine($"ContentViewer_PointerWheelChanged: {properties.MouseWheelDelta}");
		// ホイール delta
		var delta = properties.MouseWheelDelta;

		var offsetChange = -delta / 40;

		this.ViewModel.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value + offsetChange);

		// ContentViewer 自体は縦スクロールしない
		e.Handled = true;
	}

	private void GrepResultLineButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is HyperlinkButton btn && long.TryParse(btn.Content?.ToString(), out var line)) {
			this.ViewModel.JumpToLineCommand.Execute(line);
		}
	}

	private async void SaveRangeButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		var startBox = (TextBox)this.FindName("StartLineBox");
		var endBox = (TextBox)this.FindName("EndLineBox");
		if (!long.TryParse(startBox.Text, out var start) || !long.TryParse(endBox.Text, out var end)) {
			return;
		}
		if (end < start) {
			return;
		}
		var lines = this.ViewModel.GetRangeContent(start, end, new CancellationToken());

		try {
			var picker = new FileSavePicker();
			var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
			InitializeWithWindow.Initialize(picker, hwnd);
			picker.FileTypeChoices.Add("Text", [".txt"]);
			picker.SuggestedFileName = $"lines_{start}_{end}";
			var file = await picker.PickSaveFileAsync();
			if (file == null) {
				return;
			}
			await foreach (var line in lines) {
				await FileIO.AppendLinesAsync(file, [line]);
			}
		} catch {
			// TODO: エラー通知
		}
	}

	private void HyperlinkButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is HyperlinkButton btn && long.TryParse(btn.Content?.ToString(), out var line)) {
			this.ViewModel.SelectedTextLine.Value = this.ViewModel.Lines.FirstOrDefault(ln => ln.LineNumber == line);
			this.BottomTabView.SelectedItem = this.SelectedLineView;
		}
	}
}
