using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

using Microsoft.Extensions.Logging;
using Microsoft.Win32;

using RemoteLogViewer.Core.Services.Viewer;
using RemoteLogViewer.Core.ViewModels.Ssh.FileViewer;
using RemoteLogViewer.WPF.Utils;

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
	private double _lineHeight = 14.05;
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
			_ = field.WindowStartLine.Subscribe(x => {
				this.VirtualScrollViewer.ScrollToVerticalOffset(x * this._lineHeight);
			});
			_ = field.Content.AsObservable().Subscribe(content => {
				this.SetHilight(this.ContentRichTextBlock, content);
			});
			_ = field.PickedupTextLine.AsObservable().Where(tl => tl != null).Subscribe(tl => {
				this.SetHilight(this.PickedupRichTextBlock, tl!.Content!);
			});
			_ = field.TotalLines.AsObservable().Subscribe(tl => {
				this.Kari.Height = tl * this._lineHeight;
			});
			_ = field.VisibleLineCount.AsObservable().Subscribe(vlc => {
				this.VirtualScrollViewer.Height = vlc * this._lineHeight;
			});
		}
	}

	private void SetHilight(RichTextBox richTextBlock, string content) {
		var paragraph = new Paragraph();
		paragraph.Inlines.Clear();

		var highlights = this._highlightService.ComputeHighlightSpans(content);

		var index = 0;

		foreach (var hs in highlights) {
			foreach (var range in hs.Ranges) {
				// ハイライト前の通常テキスト
				if (range.StartIndex > index) {
					var normal = content.Substring(index, range.StartIndex - index);
					paragraph.Inlines.Add(new Run(normal));
				}

				// ハイライト部分
				var highlightText = content.Substring(range.StartIndex, range.Length);
				var run = new Run(highlightText);

				if (hs.Style.ForeColor is { } fore) {
					run.Foreground = this.GetBrush(fore.ToColor());
				}

				if (hs.Style.BackColor is { } back) {
					run.Background = this.GetBrush(back.ToColor());
				}

				paragraph.Inlines.Add(run);

				index = range.StartIndex + range.Length;
			}
		}

		// 残り
		if (index < content.Length) {
			var tail = content[index..];
			paragraph.Inlines.Add(new Run(tail));
		}

		richTextBlock.Document.Blocks.Clear();
		richTextBlock.Document.Blocks.Add(paragraph);
	}


	public TextFileViewer() {
		this._highlightService = Ioc.Default.GetRequiredService<HighlightService>();
		this.InitializeComponent();
		this.DataContextChanged += (_, _2) => {
			if (this.DataContext is TextFileViewerViewModel vm) {
				this.ViewModel = vm;
			}
		};
	}

	private void TextBlock_SizeChanged(object sender, SizeChangedEventArgs e) {
		if (this._lineHeight == e.NewSize.Height) {
			return;
		}
		this._lineHeight = e.NewSize.Height;
	}

	private void ContentViewer_SizeChanged(object sender, SizeChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		this.logger.LogTrace($"ContentViewer_SizeChanged: {e.NewSize.Height}");
		var visibleLines = Math.Max(1, (int)Math.Floor(e.NewSize.Height / this._lineHeight));
		this.ViewModel.VisibleLineCount.Value = visibleLines;
	}

	private void VirtualScrollViewer_ViewChanged(object sender, ScrollChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		this.logger.LogTrace($"VirtualScrollViewer_ViewChanged: {this.VirtualScrollViewer.VerticalOffset}");
		this.ViewModel.JumpToLineCommand.Execute((long)Math.Ceiling(this.VirtualScrollViewer.VerticalOffset / this._lineHeight));
	}

	private void ContentViewer_MouseWheel(object sender, MouseWheelEventArgs e) {
		this.logger.LogTrace($"ContentViewer_PointerWheelChanged: {e.Delta}");
		this.ScrollContent(e.Delta);

		e.Handled = true;
	}
	private void ContentRichTextBlock_ManipulationDelta(object sender, ManipulationDeltaEventArgs e) {
		this.logger.LogTrace($"ContentRichTextBlock_ManipulationDelta: {e.DeltaManipulation.Translation.Y}");
		this.ScrollContent((int)Math.Floor(e.DeltaManipulation.Translation.Y));
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
		if (sender is Hyperlink btn && long.TryParse(btn?.ToString(), out var line)) {
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
			var sfd = new SaveFileDialog {
				Filter = ".txt|*.*",
				FileName = $"lines_{start}_{end}"
			};
			if (!(sfd.ShowDialog() ?? false)) {
				return;
			}
			using var stream = File.OpenWrite(sfd.FileName);
			using var writer = new StreamWriter(stream);
			await this.ViewModel.SaveRangeContent(writer, start, end);
		} catch {
			// TODO: エラー通知
		}
	}

	private void LineNumber_Click(object sender, MouseButtonEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		if (sender is Hyperlink btn && long.TryParse(btn?.ToString(), out var line)) {
			this.ViewModel.PickupTextLineCommand.Execute(line);
			this.BottomTabControl.SelectedItem = this.SelectedLineView;
		}
	}

	private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		this.ViewModel.ChangeEncodingCommand.Execute(Unit.Default);

	}

	private void ContentRichTextBlock_KeyDown(object sender, KeyEventArgs e) {
		switch (e.Key) {
			case Key.Down:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value + 1);
				e.Handled = true;
				break;
			case Key.Up:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value - 1);
				e.Handled = true;
				break;
			case Key.PageDown:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value + this.ViewModel.VisibleLineCount.Value);
				e.Handled = true;
				break;
			case Key.PageUp:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.WindowStartLine.Value - this.ViewModel.VisibleLineCount.Value);
				e.Handled = true;
				break;
			case Key.Home:
				this.ViewModel?.JumpToLineCommand.Execute(1);
				e.Handled = true;
				break;
			case Key.End:
				this.ViewModel?.JumpToLineCommand.Execute(this.ViewModel.TotalLines.Value - this.ViewModel.VisibleLineCount.Value + 1);
				e.Handled = true;
				break;
		}
	}
}
