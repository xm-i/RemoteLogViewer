using Microsoft.Extensions.DependencyInjection;

using RemoteLogViewer.Models.Ssh;
using RemoteLogViewer.Services;
using RemoteLogViewer.Services.Ssh; // for SshEntry

namespace RemoteLogViewer.ViewModels.Ssh;

/// <summary>
///     SSH 接続設定と接続後の状態管理を行います。
/// </summary>
[AddScoped]
public class SshBrowserViewModel: BaseSshPageViewModel {
	private readonly SshSessionModel _model;

	/// <summary>
	///     ディレクトリエントリ一覧。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<FileSystemObject> Entries {
		get;
	}

	/// <summary>
	///     現在のパス。
	/// </summary>
	public BindableReactiveProperty<string> CurrentPath {
		get;
	}

	/// <summary>開いているファイルのパス。</summary>
	public IReadOnlyBindableReactiveProperty<string?> OpenedFilePath { get; }
	/// <summary>開いているファイル内容。</summary>
	public IReadOnlyBindableReactiveProperty<string?> FileContent { get; }

	/// <summary>
	///     切断コマンド。
	/// </summary>
	public ReactiveCommand DisconnectCommand { get; } = new();

	/// <summary>
	/// パスナビゲートコマンド (テキストボックス編集適用)。
	/// </summary>
	public ReactiveCommand NavigatePathCommand { get; } = new();

	/// <summary>ファイルシステムオープンコマンド。</summary>
	public ReactiveCommand<FileSystemObject> OpenCommand { get; } = new();

	public SshBrowserViewModel(SshSessionModel model) {
		this._model = model;
		this.CurrentPath = this._model.CurrentPath!.ToBindableReactiveProperty()!;
		this.Entries = this._model.Entries.ToNotifyCollectionChanged();
		this.OpenedFilePath = this._model.OpenedFilePath.ToReadOnlyBindableReactiveProperty();
		this.FileContent = this._model.FileContent.ToReadOnlyBindableReactiveProperty();
		this.DisconnectCommand.Subscribe(_ => this._model.Disconnect());
		this.OpenCommand.Subscribe(fso => {
			switch (fso?.FileSystemObjectType) {
				case FileSystemObjectType.Directory:
				case FileSystemObjectType.Symlink:
					this._model.EnterDirectory(fso.FileName);
					break;
				case FileSystemObjectType.File:
					this._model.OpenFile(fso);
					break;
			}
		});
		this.NavigatePathCommand.Subscribe(_ => {
			this._model.NavigateTo(this.CurrentPath.Value);
		});
	}
}
