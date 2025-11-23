using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Documents;

using Microsoft.Extensions.Logging;
using RemoteLogViewer.Core.Services.Viewer;
using RemoteLogViewer.Core.ViewModels.Ssh.FileViewer;
using RemoteLogViewer.WPF.Utils;
using TextRange = Microsoft.UI.Xaml.Documents.TextRange;

namespace RemoteLogViewer.WPF.Views.Ssh.FileViewer;

/// <summary>
///     テキストファイルビューア。スクロール位置に応じて行を部分的に読み込みます。
/// </summary>
public sealed partial class TextFileViewer {
	private ILogger<TextFileViewer> logger {
		get {
			return field ??= App.LoggerFactory.CreateLogger<TextFileViewer>();
		}
	}
	private readonly HighlightService _highlightService;
	private readonly SolidColorBrush _transparentColorBrush = new(Color.FromArgb(0, 0, 0, 0));
	private readonly ConcurrentDictionary<Color, SolidColorBrush> _brushCache = [];
	private SolidColorBrush GetBrush(Color c) {
		if (this._brushCache.TryGetValue(c, out var b)) {
			return b;
		}
		b = new SolidColorBrush(c);
		this._brushCache[c] = b;
		return b;
	}

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
			field.Content.AsObservable().Subscribe(content => {
				this.SetHilight(this.ContentRichTextBlock, content);
			});
			field.PickedupTextLine.AsObservable().Where(tl => tl != null).Subscribe(tl => {
				this.SetHilight(this.PickedupRichTextBlock, tl!.Content!);
			});
		}
	}

	private void SetHilight(RichTextBlock richTextBlock, string content) {
		richTextBlock.TextHighlighters.Clear();
		var hss = this._highlightService.ComputeHighlightSpans(content);
		foreach (var hs in hss) {
			var th = new TextHighlighter() {
				Foreground = hs.Style.ForeColor is { } fore ? this.GetBrush(fore.ToColor()) : null,
				Background = hs.Style.BackColor is { } back ? this.GetBrush(back.ToColor()) : this._transparentColorBrush
			};
			foreach (var range in hs.Ranges) {
				th.Ranges.Add(new TextRange(range.StartIndex, range.Length));
			}
			richTextBlock.TextHighlighters.Add(th);
		}
	}


	private const long LineHeight = 16;
	public TextFileViewer() {
		this._highlightService = Ioc.Default.GetRequiredService<HighlightService>();
		this.InitializeComponent();
	}

	private void ContentViewer_SizeChanged(object sender, SizeChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		this.logger.LogTrace($"ContentViewer_SizeChanged: {e.NewSize.Height}");
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
		this.logger.LogTrace($"VirtualScrollViewer_ViewChanged: {this.VirtualScrollViewer.VerticalOffset}");
		this.ViewModel.JumpToLineCommand.Execute((long)this.VirtualScrollViewer.VerticalOffset / LineHeight);
	}

	private void ContentViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e) {
		var properties = e.GetCurrentPoint(this.ContentViewer).Properties;
		if (properties.IsHorizontalMouseWheel) {
			return;
		}
		this.logger.LogTrace($"ContentViewer_PointerWheelChanged: {properties.MouseWheelDelta}");
		this.ScrollContent(properties.MouseWheelDelta);

		e.Handled = true;
	}
	private void ContentRichTextBlock_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) {
		if (e.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse) {
			return;
		}
		this.logger.LogTrace($"ContentRichTextBlock_ManipulationDelta: {e.Delta.Translation.Y}");
		this.ScrollContent((int)Math.Floor(e.Delta.Translation.Y));
		e.Handled = true;
	}

	private void ScrollContent(int delta) {
		if (this.ViewModel == null) {
			return;
		}

		var offsetChange = -delta / 40;

		this.ViewModel.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value + offsetChange);

	}

	private void GrepResultLineButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is Hyperlink btn && long.TryParse(btn.Content?.ToString(), out var line)) {
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
		try {
			var picker = new FileSavePicker();
			var hwnd = WindowNative.GetWindowHandle(WinUI.App.MainWindow);
			InitializeWithWindow.Initialize(picker, hwnd);
			picker.FileTypeChoices.Add("Text", [".txt"]);
			picker.SuggestedFileName = $"lines_{start}_{end}";
			var file = await picker.PickSaveFileAsync();
			if (file == null) {
				return;
			}
			using var stream = await file.OpenStreamForWriteAsync();
			using var writer = new StreamWriter(stream);
			await this.ViewModel.SaveRangeContent(writer, start, end);
		} catch {
			// TODO: エラー通知
		}
	}

	private void HyperlinkButton_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is HyperlinkButton btn && long.TryParse(btn.Content?.ToString(), out var line)) {
			this.ViewModel.PickupTextLineCommand.Execute(line);
			this.BottomTabView.SelectedItem = this.SelectedLineView;
		}
	}

	private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
		if (this.ViewModel == null) {
			return;
		}
		this.ViewModel.ChangeEncodingCommand.Execute(Unit.Default);

	}

	private void ContentRichTextBlock_KeyDown(object sender, KeyRoutedEventArgs e) {
		switch (e.Key) {
			case Windows.System.VirtualKey.Down:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value + 1);
				e.Handled = true;
				break;
			case Windows.System.VirtualKey.Up:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value - 1);
				e.Handled = true;
				break;
			case Windows.System.VirtualKey.PageDown:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value + this.ViewModel.VisibleLineCount.Value);
				e.Handled = true;
				break;
			case Windows.System.VirtualKey.PageUp:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value - this.ViewModel.VisibleLineCount.Value);
				e.Handled = true;
				break;
			case Windows.System.VirtualKey.Home:
				this.ViewModel?.JumpToLineCommand.Execute(1);
				e.Handled = true;
				break;
			case Windows.System.VirtualKey.End:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.TotalLines.Value - this.ViewModel.VisibleLineCount.Value + 1);
				e.Handled = true;
				break;
		}
	}

}
