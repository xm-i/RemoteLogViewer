using R3;

using RemoteLogViewer.Models.Ssh;
using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.ViewModels.Ssh.FileViewer; // for SshEntry

namespace RemoteLogViewer.ViewModels.Ssh;

/// <summary>
///     SSH 接続設定と接続後の状態管理を行います。
/// </summary>
[AddScoped]
public class SshBrowserViewModel : BaseSshPageViewModel {
	private readonly SshSessionModel _model;
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
	public ReactiveCommand ReconnectCommand { get; }

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
	public TextFileViewerViewModel TextFileViewerViewModel {
		get;
	}

	public SshBrowserViewModel(SshSessionModel model, TextFileViewerViewModel textFileViewerViewModel) {
		this._model = model;
		this.TextFileViewerViewModel = textFileViewerViewModel;
		this.CurrentPath = this._model.CurrentPath!.ToBindableReactiveProperty()!.AddTo(this.CompositeDisposable)!;
		var view = this._model.Entries.CreateView(f => new FileSystemEntryViewModel(f, this._model)).AddTo(this.CompositeDisposable);
		this.Entries = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this._model.SelectedSshConnectionInfo.Subscribe(x => {
			if (x == null) {
				return;
			}
			var bmView = x.Bookmarks.CreateView(b => b).AddTo(this.CompositeDisposable);
			this.Bookmarks = bmView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		}).AddTo(this.CompositeDisposable);
		this.DisconnectCommand.Subscribe(_ => this._model.Disconnect()).AddTo(this.CompositeDisposable);
		this.DisconnectedWithException = this._model.DisconnectedWithException.ToReadOnlyBindableReactiveProperty(false).AddTo(this.CompositeDisposable);

		this.ReconnectCommand = this.DisconnectedWithException.AsObservable().ToReactiveCommand(_ => { this._model.Reconnect(); }).AddTo(this.CompositeDisposable);

		this.OpenCommand
			.Where(vm => vm?.FileSystemObjectType == FileSystemObjectType.File || vm?.FileSystemObjectType == FileSystemObjectType.SymlinkFile)
			.SubscribeAwait(async (vm, ct) => {

				await this.TextFileViewerViewModel.OpenFileAsync(this.CurrentPath.Value, vm.Original, ct);
		}, AwaitOperation.Switch).AddTo(this.CompositeDisposable);

		this.OpenCommand
			.Where(vm => vm?.FileSystemObjectType == FileSystemObjectType.Directory || vm?.FileSystemObjectType == FileSystemObjectType.SymlinkDirectory)
			.Subscribe(vm => {
				this._model.EnterDirectory(vm.FileName);
			}).AddTo(this.CompositeDisposable);

		this.OpenBookmarkCommand.Subscribe(bm => {
			if (bm == null) {
				return;
			}
			this._model.NavigateTo(bm.Path.Value);
		}).AddTo(this.CompositeDisposable);
		this.NavigatePathCommand.Subscribe(_ => {
			this._model.NavigateTo(this.CurrentPath.Value);
		}).AddTo(this.CompositeDisposable);
		this.EntryFilterWord.ThrottleLast(TimeSpan.FromMilliseconds(100), ObservableSystem.DefaultTimeProvider).Subscribe(_ => {
		var word = this.EntryFilterWord.Value;
		if (string.IsNullOrWhiteSpace(word)) {
			view.ResetFilter();
		} else {
			view.AttachFilter(vm => vm.FileName.Contains(word!, StringComparison.CurrentCultureIgnoreCase));
		}
	}).AddTo(this.CompositeDisposable);
	}
}
