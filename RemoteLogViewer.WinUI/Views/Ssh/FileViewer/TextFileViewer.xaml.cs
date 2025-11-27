using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using RemoteLogViewer.Core.Services.Viewer;
using RemoteLogViewer.Core.ViewModels.Ssh.FileViewer;

using Windows.Storage.Pickers;

using WinRT.Interop;

namespace RemoteLogViewer.WinUI.Views.Ssh.FileViewer;

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

	public TextFileViewerViewModel? ViewModel {
		get;
		set {
			field = value;
			if (field == null) {
				return;
			}

			_ = field.PickedupTextLine.AsObservable().Where(tl => tl != null).Subscribe(tl => {
				//this.SetHilight(this.PickedupRichTextBlock, tl!.Content!);
			});
		}
	}

	private async Task SetHighlight(WebView2 webView, string content) {
		var htmlBodyContent = this.BuildHtmlBody(content);
		var js = $"document.getElementById('content').innerHTML = {JsonSerializer.Serialize(htmlBodyContent)};";
		_ = await webView.ExecuteScriptAsync(js);
	}

	private string BuildHtmlBody(string content) {
		var hss = this._highlightService.ComputeHighlightSpans(content);
		var sb = new StringBuilder();

		var index = 0;

		foreach (var hs in hss) {
			foreach (var range in hs.Ranges.OrderBy(r => r.StartIndex)) {
				if (index < range.StartIndex) {
					_ = sb.Append(Escape(content.Substring(index, range.StartIndex - index)));
				}

				var text = content.Substring(range.StartIndex, range.Length);

				_ = sb.Append($"<span style=\"background:{hs.Style.BackColor};color:{hs.Style.ForeColor};\">");
				_ = sb.Append(Escape(text));
				_ = sb.Append("</span>");

				index = range.StartIndex + range.Length;
			}
		}

		if (index < content.Length) {
			_ = sb.Append(Escape(content.Substring(index)));
		}

		return sb.ToString();
	}

	private static string Escape(string s) {
		return System.Net.WebUtility.HtmlEncode(s);
	}

	public TextFileViewer() {
		this._highlightService = Ioc.Default.GetRequiredService<HighlightService>();
		this.InitializeComponent();
		this.ContentWebViewer.Loaded += async (_, _2) => {
			await this.InitializeWebView();
		};
	}

	/// <summary>
	/// WebViewの初期化・イベント発生時処理登録
	/// </summary>
	private async Task InitializeWebView() {
		if (this.ViewModel == null) {
			return;
		}
		await this.ContentWebViewer.EnsureCoreWebView2Async();
		this.ContentWebViewer.CoreWebView2.SetVirtualHostNameToFolderMapping("app", Path.Combine(AppContext.BaseDirectory, "Assets", "Web"), CoreWebView2HostResourceAccessKind.Allow);
		this.ContentWebViewer.CoreWebView2.Navigate("https://app/index.html");

		void post(string type, dynamic data) {
			var message = new {
				type,
				data
			};
			this.ContentWebViewer.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(message));
		}

		this.ContentWebViewer.CoreWebView2.WebMessageReceived += (s, e) => {
			var message = WebMessage.Create(e.WebMessageAsJson);

			switch (message) {
				case RequestWebMessage rwm:
					this.ViewModel.LoadLogsCommand.Execute(new(rwm.Start, rwm.End));
					break;
			}
		};

		_ = this.ViewModel.Loaded.Subscribe(x => {
			post("Loaded", x);
		});

		_ = this.ViewModel.OpenedFilePath.AsObservable().Subscribe(x => {
			if (x == null) {
				return;
			}
			post("FileChanged", x);
		});
		_ = this.ViewModel.ReloadRequested.AsObservable().Subscribe(x => {
			post("ReloadRequested", x);
		});
		_ = this.ViewModel.TotalLines.AsObservable().Subscribe(async x => {
			post("TotalLinesUpdated", x);
		});
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
}