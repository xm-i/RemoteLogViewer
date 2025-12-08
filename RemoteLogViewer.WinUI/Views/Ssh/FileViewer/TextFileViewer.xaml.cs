using System.Collections.Specialized;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;

using RemoteLogViewer.Core.Models.Ssh.FileViewer;
using RemoteLogViewer.Core.Services;
using RemoteLogViewer.Core.Services.Viewer;
using RemoteLogViewer.Core.Stores.Settings;
using RemoteLogViewer.Core.ViewModels.Ssh;
using RemoteLogViewer.Core.ViewModels.Ssh.FileViewer;

using Windows.Foundation;

using WinRT.Interop;

namespace RemoteLogViewer.WinUI.Views.Ssh.FileViewer;

/// <summary>
///     テキストファイルビューア。スクロール位置に応じて行を部分的に読み込みます。
/// </summary>
public sealed partial class TextFileViewer {
	private readonly HighlightService _highlightService;
	private readonly SettingsStoreModel _settingsStoreModel;
	private readonly NotificationService _notificationService;
	private readonly ILogger<TextFileViewer> _logger;
	private bool isInitialized = false;

	public SshBrowserViewModel? ViewModel {
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
		this._logger = Ioc.Default.GetRequiredService<ILogger<TextFileViewer>>();
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
#if RELEASE || RELEASE_UNPACKAGED
		this.ContentWebViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
		this.ContentWebViewer.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
		this.ContentWebViewer.CoreWebView2.SetVirtualHostNameToFolderMapping("app", Path.Combine(AppContext.BaseDirectory, "Assets", "Web","dist"), CoreWebView2HostResourceAccessKind.Allow);
		this.ContentWebViewer.CoreWebView2.Navigate("https://app/index.html");


		Observable.FromEvent<TypedEventHandler<CoreWebView2, CoreWebView2WebMessageReceivedEventArgs>, CoreWebView2WebMessageReceivedEventArgs>(h => (sender, e) => h(e),
			h => this.ContentWebViewer.CoreWebView2.WebMessageReceived += h,
			h => this.ContentWebViewer.CoreWebView2.WebMessageReceived -= h)
			.SubscribeAwait(async (e, ct) => {
				this._logger.LogDebug("WebMessageReceived: {Message}", e.WebMessageAsJson);
				var message = WebMessage.Create(e.WebMessageAsJson);
				switch (message) {
					case RequestWebMessage m:
						this.GetViewerVM(m.pageKey)?.LoadLogsCommand.Execute(new(m.requestId, m.start, m.end));
						break;
					case StartGrepWebMessage m:
						var vm = this.GetViewerVM(m.pageKey);
						if (vm is null) {
							return;
						}
						vm.GrepStartLine.Value = m.startLine;
						vm.GrepQuery.Value = m.keyword;
						vm.GrepUseRegex.Value = m.useRegex;
						vm.GrepIgnoreCase.Value = m.ignoreCase;
						vm.GrepCommand.Execute(Unit.Default);
						break;
					case CancelGrepWebMessage:
						this.GetViewerVM(message.pageKey)?.GrepCancelCommand.Execute(Unit.Default);
						break;
					case ReadyWebMessage:
						this.PostWV2("*", "LineStyleChanged", this._highlightService.CreateCss());
						this.ViewModel.IsViewerReady.Value = true;
						break;
					case SaveRangeRequestWebMessage m:
						var srrvm = this.GetViewerVM(m.pageKey);
						if (srrvm is null) {
							return;
						}
						if (m.end < m.start) {
							return;
						}
						try {
							var picker = new Windows.Storage.Pickers.FileSavePicker();
							var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
							InitializeWithWindow.Initialize(picker, hwnd);
							picker.FileTypeChoices.Add("Text", [".txt"]);
							picker.SuggestedFileName = $"lines_{m.start}_{m.end}";
							var file = await picker.PickSaveFileAsync();
							if (file == null) {
								return;
							}
							using var stream = await file.OpenStreamForWriteAsync();
							using var writer = new StreamWriter(stream);
							await srrvm.SaveRangeContent(writer, m.start, m.end);
						} catch {
							// TODO: エラー通知
						}
						break;
					case ChangeEncodingWebMessage m:
						this.GetViewerVM(m.pageKey)?.ChangeEncodingCommand.Execute(m.encoding);
						break;
					case UpdateTotalLineWebMessage m:
						this.GetViewerVM(m.pageKey)?.UpdateTotalLineCommand.Execute(Unit.Default);
						break;
					case FileCloseWebMessage m:
						var removeTarget = this.GetViewerVM(m.pageKey);
						if (removeTarget is null) {
							return;
						}
						this.ViewModel.CloseFileViewerCommand.Execute(removeTarget);
						break;
				}
			}
		);

		this.ViewModel.DisconnectedWithException.AsObservable().Subscribe(x => {
			this.PostWV2("*", "IsDisconnectedUpdated", x);
		}).AddTo(this.ViewModel.CompositeDisposable);

		this._settingsStoreModel.SettingsUpdated.Subscribe(x => {
			this.PostWV2("*", "LineStyleChanged", this._highlightService.CreateCss());
			this.PostWV2("*", "ReloadRequested", null);
			this.PostWV2("*", "GrepResultReset", null);
		});

		Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
			h => (sender, e) => h(e),
			h => this.ViewModel.OpenedFileViewerViewModels.CollectionChanged += h,
			h => this.ViewModel.OpenedFileViewerViewModels.CollectionChanged -= h
		).Subscribe(e => {
			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
				case NotifyCollectionChangedAction.Remove:
					if (e.NewItems is not null) {
						foreach (TextFileViewerViewModel vm in e.NewItems) {
							this.PostWV2("*", "FileOpened", new {
								pageKey = vm.PageKey,
								tabHeader = Path.GetFileName(vm.OpenedFilePath.Value)
							});
							this.RegisterViewerVMEvents(vm);
						}
					}
					if (e.OldItems is not null) {
						foreach (TextFileViewerViewModel vm in e.OldItems) {
							this.PostWV2("*", "FileClosed", vm.PageKey);
						}
					}
					break;
			}
		}).AddTo(this.ViewModel.CompositeDisposable);
	}

	private void RegisterViewerVMEvents(TextFileViewerViewModel vm) {
		// メインログビューイベント
		_ = vm.Loaded.Subscribe(x => {
			this.PostWV2(vm.PageKey, "Loaded", new {
				requestId = x.RequestId,
				content = x.Content.Select(x => new {
					lineNumber = x.LineNumber,
					content = this._highlightService.CreateStyledLine(x.Content)
				})
			});
		}).AddTo(vm.CompositeDisposable);

		_ = vm.OpenedFilePath.AsObservable().Subscribe(x => {
			if (x == null) {
				return;
			}
			this.PostWV2(vm.PageKey, "FileChanged", x);
		}).AddTo(vm.CompositeDisposable);
		_ = vm.ReloadRequested.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "ReloadRequested", null);
		}).AddTo(vm.CompositeDisposable);
		_ = vm.TotalLines.AsObservable().Subscribe(async x => {
			this.PostWV2(vm.PageKey, "TotalLinesUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		// ファイル操作エリアイベント
		_ = vm.FileLoadProgress.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "FileLoadProgressUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		_ = vm.IsFileLoadRunning.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "IsFileLoadRunningUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		_ = vm.TotalBytes.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "TotalBytesUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		_ = vm.OpenedFilePath.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "OpenedFilePathChanged", x);
		}).AddTo(vm.CompositeDisposable);

		_ = vm.IsRangeContentSaving.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "IsRangeContentSavingUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		_ = vm.SaveRangeProgress.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "SaveRangeProgressUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		_ = vm.AvailableEncodings.ObservePropertyChanged(x => x.Count).ThrottleLast(TimeSpan.FromMilliseconds(100)).Subscribe(x => {
			this.PostWV2(vm.PageKey, "AvailableEncodingsUpdated", vm.AvailableEncodings);
		}).AddTo(vm.CompositeDisposable);

		// GREPタブイベント
		_ = vm.GrepProgress.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "GrepProgressUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		_ = vm.GrepStartLine.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "GrepStartLineUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		_ = vm.IsGrepRunning.AsObservable().Subscribe(x => {
			this.PostWV2(vm.PageKey, "IsGrepRunningUpdated", x);
		}).AddTo(vm.CompositeDisposable);

		_ = Observable.FromEvent<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
			h => (sender, e) => h(e),
			h => vm.GrepResults.CollectionChanged += h,
			h => vm.GrepResults.CollectionChanged -= h
		).Subscribe(e => {
			switch (e.Action) {
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems is null) {
						return;
					}
					this.PostWV2(vm.PageKey, "GrepResultAdded", e.NewItems.Cast<TextLine>().Select(x => new { lineNumber = x.LineNumber, content = this._highlightService.CreateStyledLine(x.Content) }));
					break;
				case NotifyCollectionChangedAction.Reset:
					this.PostWV2(vm.PageKey, "GrepResultReset", null);
					break;
			}
		}).AddTo(vm.CompositeDisposable);
	}

	private void PostWV2(string pageKey, string type, dynamic? data) {
		var message = new {
			pageKey,
			type,
			data
		};
		var json = JsonSerializer.Serialize(message);
		this.ContentWebViewer.CoreWebView2.PostWebMessageAsJson(json);
		this._logger.LogDebug("PostWV2: {Message}", json);
	}

	private TextFileViewerViewModel? GetViewerVM(string key) {
		if (this.ViewModel == null) {
			throw new InvalidOperationException();
		}

		return this.ViewModel.OpenedFileViewerViewModels.FirstOrDefault(x => x.PageKey == key);
	}

	public void Reset() {
		if (!this.isInitialized || this.ViewModel is null) {
			return;
		}
		this.ViewModel.IsViewerReady.Value = false;
		this.ContentWebViewer.CoreWebView2.Reload();
	}
}