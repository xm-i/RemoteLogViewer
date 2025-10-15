using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.ViewModels.Ssh.FileViewer;

namespace RemoteLogViewer.Views.Ssh.FileViewer;

/// <summary>
///     テキストファイルビューア。スクロール位置に応じて行を部分的に読み込みます。
/// </summary>
public sealed partial class TextFileViewer {
	public TextFileViewerViewModel? ViewModel {
		get; set;
	}
	private const long LineHeight = 18;
	public TextFileViewer() {
		this.InitializeComponent();
	}

	private void VirtualScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		var visibleLines = Math.Max(1, (int)(e.NewSize.Height / LineHeight));
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
		this.ViewModel.WindowStartLine.Value = (long)(this.VirtualScrollViewer.VerticalOffset / LineHeight);
		this.ViewModel.LoadLinesCommand.Execute(Unit.Default);
	}

	private void ContentViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
		var properties = e.GetCurrentPoint(this.ContentViewer).Properties;
		if (properties.IsHorizontalMouseWheel) {
			return;
		}
		// ホイール delta
		var delta = properties.MouseWheelDelta;

		// 1行分のスクロール量に変換（LineHeight をピクセル単位で定義）
		var offsetChange = -delta / 120.0 * LineHeight;

		var newOffset = this.VirtualScrollViewer.VerticalOffset + offsetChange;

		// ScrollableHeight を超えないように制限
		newOffset = Math.Max(0, Math.Min(newOffset, this.VirtualScrollViewer.ScrollableHeight));

		// VirtualScrollViewer に反映
		this.VirtualScrollViewer.ChangeView(null, newOffset, null, true);

		// ContentViewer 自体は縦スクロールしない
		e.Handled = true;
	}

	private void GrepResultLineButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is HyperlinkButton btn && long.TryParse(btn.Content?.ToString(), out var line)) {
			this.ViewModel.JumpToLineCommand.Execute(line);
			this.VirtualScrollViewer.ScrollToVerticalOffset(this.ViewModel.WindowStartLine.Value * LineHeight);
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
		var content = this.ViewModel.GetRangeContent(start, end);
		if (string.IsNullOrEmpty(content)) {
			return;
		}

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
			await FileIO.WriteTextAsync(file, content);
		} catch {
			// TODO: エラー通知
		}
	}
}
