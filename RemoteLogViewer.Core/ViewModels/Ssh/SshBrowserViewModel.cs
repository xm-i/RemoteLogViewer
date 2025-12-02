using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RemoteLogViewer.Composition.Stores.Ssh;
using RemoteLogViewer.Core.Models.Ssh;
using RemoteLogViewer.Core.Services;
using RemoteLogViewer.Core.Services.Ssh;
using RemoteLogViewer.Core.Utils;
using RemoteLogViewer.Core.ViewModels.Ssh.FileViewer;
// for SshEntry

namespace RemoteLogViewer.Core.ViewModels.Ssh;

/// <summary>
///     SSH 接続設定と接続後の状態管理を行います。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class SshBrowserViewModel : BaseSshPageViewModel<SshBrowserViewModel> {
	private readonly SshSessionModel _model;
	private readonly NotificationService _notificationService;
	/// <summary>
	///     ディレクトリエントリ一覧 (ViewModel)。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<FileSystemEntryViewModel> Entries {
		get;
	}

	/// <summary>現在の接続のブックマーク一覧。</summary>
	public NotifyCollectionChangedSynchronizedViewList<SshBookmarkModel>? Bookmarks {
		get;
		private set;
	}

	/// <summary>
	///     現在のパス。
	/// </summary>
	public BindableReactiveProperty<string> CurrentPath {
		get;
	}

	/// <summary>
	/// 現在のパスがブックマークされているか。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<bool> IsCurrentDirectoryBookmarked {
		get;
	}

	public ReactiveCommand<bool> ToggleCurrentDirectoryBookmarkCommand {
		get;
	} = new();

	/// <summary>
	/// エントリフィルターワード。
	/// </summary>
	public BindableReactiveProperty<string?> EntryFilterWord {
		get;
	} = new();

	/// <summary>
	///     切断コマンド。
	/// </summary>
	public ReactiveCommand DisconnectCommand { get; } = new();

	public IReadOnlyBindableReactiveProperty<bool> DisconnectedWithException {
		get;
	}

	/// <summary>
	/// 再接続コマンド。
	/// </summary>
	public ReactiveCommand ReconnectCommand {
		get;
	}

	/// <summary>
	/// パスナビゲートコマンド。
	/// </summary>
	public ReactiveCommand NavigatePathCommand { get; } = new();

	/// <summary>ファイルシステムオープンコマンド。</summary>
	public ReactiveCommand<FileSystemEntryViewModel> OpenCommand {
		get;
	} = new();

	/// <summary>ブックマークオープンコマンド。</summary>
	public ReactiveCommand<SshBookmarkModel> OpenBookmarkCommand { get; } = new();

	/// <summary>
	/// テキストファイル閲覧 ViewModel。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<TextFileViewerViewModel> OpenedFileViewerViewModels {
		get;
	}

	/// <summary>
	/// Viewerが準備完了しているか否か
	/// </summary>
	public BindableReactiveProperty<bool> IsViewerReady {
		get;
	} = new(false);


	public SshBrowserViewModel(SshSessionModel model, NotificationService notificationService, ILogger<SshBrowserViewModel> logger) : base(logger) {
		this._model = model.AddTo(this.CompositeDisposable);
		this._notificationService = notificationService;
		this.OpenedFileViewerViewModels = this._model.OpenedFileViewers.CreateView(x => x.ServiceProvider.GetRequiredService<TextFileViewerViewModel>()).ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.CurrentPath = this._model.CurrentPath!.ToBindableReactiveProperty()!.AddTo(this.CompositeDisposable)!;
		this.IsCurrentDirectoryBookmarked = this._model.IsCurrentDirectoryBookmarked.ToReadOnlyBindableReactiveProperty(false).AddTo(this.CompositeDisposable);

		var view = this._model.Entries.CreateView(f => new FileSystemEntryViewModel(f, this._model)).AddTo(this.CompositeDisposable);
		this.Entries = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		_ = this._model.SelectedSshConnectionInfo.Subscribe(x => {
			if (x == null) {
				return;
			}
			var bmView = x.Bookmarks.CreateView(b => b).AddTo(this.CompositeDisposable);
			this.Bookmarks = bmView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		}).AddTo(this.CompositeDisposable);
		_ = this.DisconnectCommand.Subscribe(_ => this._model.Disconnect()).AddTo(this.CompositeDisposable);
		this.DisconnectedWithException = this._model.DisconnectedWithException.ToReadOnlyBindableReactiveProperty(false).AddTo(this.CompositeDisposable);

		this.ReconnectCommand = this.DisconnectedWithException.AsObservable().ToReactiveCommand(_ => { this._model.Reconnect(); }).AddTo(this.CompositeDisposable);

		_ = this.OpenCommand
			.Where(vm => vm?.FileSystemObjectType == FileSystemObjectType.File || vm?.FileSystemObjectType == FileSystemObjectType.SymlinkFile)
			.SubscribeAwait(async (vm, ct) => {
				if (!this.IsViewerReady.Value) {
					this._notificationService.Publish("TextFileViewer", "The viewer is initializing. Please wait.", NotificationSeverity.Warning);
					return;
				}

				await this._model.OpenFileAsync(this.CurrentPath.Value, vm.Original, ct);
			}, AwaitOperation.Sequential).AddTo(this.CompositeDisposable);

		_ = this.OpenCommand
			.Where(vm => vm?.FileSystemObjectType == FileSystemObjectType.Directory || vm?.FileSystemObjectType == FileSystemObjectType.SymlinkDirectory)
			.Subscribe(vm => {
				this._model.EnterDirectory(vm.FileName);
			}).AddTo(this.CompositeDisposable);

		_ = this.OpenBookmarkCommand.Subscribe(bm => {
			if (bm == null) {
				return;
			}
			this._model.NavigateTo(bm.Path.Value);
		}).AddTo(this.CompositeDisposable);
		_ = this.NavigatePathCommand.Subscribe(_ => {
			this._model.NavigateTo(this.CurrentPath.Value);
		}).AddTo(this.CompositeDisposable);
		_ = this.EntryFilterWord.ThrottleLast(TimeSpan.FromMilliseconds(100), ObservableSystem.DefaultTimeProvider).Subscribe(_ => {
			var word = this.EntryFilterWord.Value;
			if (string.IsNullOrWhiteSpace(word)) {
				view.ResetFilter();
			} else {
				view.AttachFilter(vm => vm.FileName.Contains(word!, StringComparison.CurrentCultureIgnoreCase));
			}
		}).AddTo(this.CompositeDisposable);

		_ = this.ToggleCurrentDirectoryBookmarkCommand.Subscribe(x => {
			if (x) {
				this._model.AddBookmark(this.CurrentPath.Value, PathUtils.GetFileOrDirectoryName(this.CurrentPath.Value)!);
			} else {
				this._model.RemoveBookmark(this.CurrentPath.Value);
			}
		}).AddTo(this.CompositeDisposable);
	}
}
