using System.Collections.Specialized;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using RemoteLogViewer.Core.Models.Ssh.FileViewer;
using RemoteLogViewer.Core.Services;
using RemoteLogViewer.Core.Services.Viewer;
using RemoteLogViewer.Core.Stores.Settings;
using RemoteLogViewer.Core.ViewModels.Ssh.FileViewer;

using Windows.Storage.Pickers;

using WinRT.Interop;

namespace RemoteLogViewer.WinUI.Views.Ssh.FileViewer;

/// <summary>
///     テキストファイルビューア。スクロール位置に応じて行を部分的に読み込みます。
/// </summary>
public sealed partial class TextFileViewer {
	private readonly HighlightService _highlightService;
	private readonly SettingsStoreModel _settingsStoreModel;
	private readonly NotificationService _notificationService;
	private bool isInitialized = false;
	public TextFileViewerViewModel? ViewModel {
		get;
		set {
			field = value;
			if (field == null) {
				return;
			}
			field.IsViewerReady.Value = false;
			var initializeWebViewTask = this.InitializeWebView();
			_ = initializeWebViewTask.ContinueWith(t => {
				if (t.IsFaulted) {
					this._notificationService.Publish("TextFileViewer", "TextFileViewer WebView2 initialization failed.", NotificationSeverity.Error, t.Exception);
					return;
				}
				this.isInitialized = true;
			});
		}
	}

	public TextFileViewer() {
		this._highlightService = Ioc.Default.GetRequiredService<HighlightService>();
		this._settingsStoreModel = Ioc.Default.GetRequiredService<SettingsStoreModel>();
		this._notificationService = Ioc.Default.GetRequiredService<NotificationService>();
		this.InitializeComponent();
	}

	/// <summary>
	/// WebViewの初期化・イベント発生時処理登録
	/// </summary>
	private async Task InitializeWebView() {
		if (this.ViewModel == null) {
			return;
		}
		await this.ContentWebViewer.EnsureCoreWebView2Async();
#if !DEBUG_UNPACKAGED
		this.ContentWebViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
		this.ContentWebViewer.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
		this.ContentWebViewer.CoreWebView2.SetVirtualHostNameToFolderMapping("app", Path.Combine(AppContext.BaseDirectory, "Assets", "Web"), CoreWebView2HostResourceAccessKind.Allow);
		this.ContentWebViewer.CoreWebView2.Navigate("https://app/index.html");

		void post(string type, dynamic? data) {
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
					this.ViewModel.LoadLogsCommand.Execute(new(rwm.RequestId, rwm.Start, rwm.End));
					break;
				case StartGrepWebMessage sgwm:
					this.ViewModel.GrepStartLine.Value = sgwm.StartLine;
					this.ViewModel.GrepQuery.Value = sgwm.Keyword;
					this.ViewModel.GrepCommand.Execute(Unit.Default);
					break;
				case CancelGrepWebMessage _:
					this.ViewModel.GrepCancelCommand.Execute(Unit.Default);
					break;
				case ReadyWebMessage _:
					post("LineStyleChanged", this._highlightService.CreateCss());
					this.ViewModel.IsViewerReady.Value = true;
					break;
			}
		};

		this._settingsStoreModel.SettingsUpdated.Subscribe(x => {
			post("LineStyleChanged", this._highlightService.CreateCss());
			post("ReloadRequested", null);
			post("GrepResultReset", null);
		});

		// メインログビューイベント
		_ = this.ViewModel.Loaded.Subscribe(x => {
			post("Loaded", new {
				x.RequestId,
				Content = x.Content.Select(x => new TextLine(x.LineNumber, Content: this._highlightService.CreateStyledLine(x.Content)))
			});
		});

		_ = this.ViewModel.OpenedFilePath.AsObservable().Subscribe(x => {
			if (x == null) {
				return;
			}
			post("FileChanged", x);
		});
		_ = this.ViewModel.ReloadRequested.AsObservable().Subscribe(x => {
			post("ReloadRequested", null);
		});
		_ = this.ViewModel.TotalLines.AsObservable().Subscribe(async x => {
			post("TotalLinesUpdated", x);
		});

		// GREPタブイベント
		_ = this.ViewModel.GrepProgress.AsObservable().Subscribe(x => {
			post("GrepProgressUpdated", x * 100);
		});

		_ = this.ViewModel.GrepStartLine.AsObservable().Subscribe(x => {
			post("GrepStartLineUpdated", x);
		});

		_ = this.ViewModel.IsGrepRunning.AsObservable().Subscribe(x => {
			post("IsGrepRunningUpdated", x);
		});

		this.ViewModel.GrepResults.CollectionChanged += (s, e) => {
			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems is null) {
						return;
					}
					post("GrepResultAdded", e.NewItems.Cast<TextLine>().Select(x => new TextLine(x.LineNumber, Content: this._highlightService.CreateStyledLine(x.Content))));
					break;
				case NotifyCollectionChangedAction.Reset:
					post("GrepResultReset", null);
					break;
			}
		};

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

	private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) {
		if (this.ViewModel == null) {
			return;
		}
		this.ViewModel.ChangeEncodingCommand.Execute(Unit.Default);
	}

	public void Reset() {
		if (!this.isInitialized || this.ViewModel is null) {
			return;
		}
		this.ViewModel.IsViewerReady.Value = false;
		this.ContentWebViewer.CoreWebView2.Reload();
	}
}