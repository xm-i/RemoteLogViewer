using Microsoft.Extensions.DependencyInjection;

using RemoteLogViewer.Models.Ssh;
using RemoteLogViewer.Services;
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

	/// <summary>
	/// パスナビゲートコマンド。
	/// </summary>
	public ReactiveCommand NavigatePathCommand { get; } = new();

	/// <summary>ファイルシステムオープンコマンド。</summary>
	public ReactiveCommand<FileSystemEntryViewModel> OpenCommand {
		get;
	} = new();

	/// <summary>ブックマーク追加コマンド。</summary>
	public ReactiveCommand<FileSystemEntryViewModel> AddBookmarkCommand {
		get;
	} = new();

	/// <summary>ブックマークトグルコマンド。</summary>
	public ReactiveCommand<FileSystemEntryViewModel> ToggleBookmarkCommand {
		get;
	} = new();

	/// <summary>
	/// テキストファイル閲覧 ViewModel。
	/// </summary>
	public TextFileViewerViewModel TextFileViewerViewModel {
		get;
	}

	public SshBrowserViewModel(SshSessionModel model, TextFileViewerViewModel textFileViewerViewModel) {
		this._model = model;
		this.TextFileViewerViewModel = textFileViewerViewModel;
		this.CurrentPath = this._model.CurrentPath!.ToBindableReactiveProperty()!;
		var view = this._model.Entries.CreateView(f => new FileSystemEntryViewModel(f, this._model));
		this.Entries = view.ToNotifyCollectionChanged();
		this.DisconnectCommand.Subscribe(_ => this._model.Disconnect());
		this.OpenCommand.Subscribe(vm => {
			var fso = vm?.Original;
			switch (fso?.FileSystemObjectType) {
				case FileSystemObjectType.Directory:
				case FileSystemObjectType.Symlink:
					this._model.EnterDirectory(fso.FileName);
					break;
				case FileSystemObjectType.File:
					this.TextFileViewerViewModel.OpenFile(this.CurrentPath.Value, fso);
					break;
			}
		});
		this.NavigatePathCommand.Subscribe(_ => {
			this._model.NavigateTo(this.CurrentPath.Value);
		});
		this.EntryFilterWord.ThrottleLast(TimeSpan.FromMilliseconds(100), ObservableSystem.DefaultTimeProvider).Subscribe(_ => {
			var word = this.EntryFilterWord.Value;
			if (string.IsNullOrWhiteSpace(word)) {
				view.ResetFilter();
			} else {
				view.AttachFilter(vm => vm.FileName.Contains(word!, StringComparison.CurrentCultureIgnoreCase));
			}
		});
	}
}
