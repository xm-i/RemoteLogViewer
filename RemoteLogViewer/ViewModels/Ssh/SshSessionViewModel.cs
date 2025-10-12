using Microsoft.Extensions.DependencyInjection;

using RemoteLogViewer.Models.Ssh;
using RemoteLogViewer.Services;
using RemoteLogViewer.Services.Ssh; // for SshEntry

namespace RemoteLogViewer.ViewModels.Ssh;

/// <summary>
///     SSH 接続設定と接続後の状態管理を行います。
/// </summary>
[AddTransient]
public class SshSessionViewModel {
	private readonly SshSessionModel _model;

	/// <summary>
	///     接続済みかどうか。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<bool> IsConnected {
		get;
	}
	/// <summary>
	///     ディレクトリエントリ一覧。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<FileSystemObject> Entries {
		get;
	}
	/// <summary>
	///     保存済み接続情報一覧。
	/// </summary>
	public NotifyCollectionChangedSynchronizedViewList<SshConnectionInfoViewModel> SavedConnections {
		get;
	}

	/// <summary>
	///     選択中接続情報。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<SshConnectionInfoViewModel?> SelectedSshConnectionInfo {
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
	///     接続コマンド。
	/// </summary>
	public ReactiveCommand ConnectCommand { get; } = new();
	/// <summary>
	///     テスト接続コマンド。
	/// </summary>
	public ReactiveCommand TestConnectCommand { get; } = new();

	/// <summary>
	/// 接続情報追加コマンド。
	/// </summary>
	public ReactiveCommand AddSavedConnectionsCommand {
		get;
	} = new();

	/// <summary>
	/// 接続情報選択コマンド。
	/// </summary>
	public ReactiveCommand<SshConnectionInfoViewModel> SelectSshConnectionInfoCommand {
		get;
	} = new();
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

	public SshSessionViewModel(SshSessionModel model) {
		this._model = model;
		this.IsConnected = this._model.IsConnected.ToReadOnlyBindableReactiveProperty();
		this.CurrentPath = this._model.CurrentPath!.ToBindableReactiveProperty()!;
		this.Entries = this._model.Entries.ToNotifyCollectionChanged();
		this.SavedConnections = this._model.SavedConnections.ToNotifyCollectionChanged(x => x.ServiceProvider.GetRequiredService<SshConnectionInfoViewModel>());
		this.SelectedSshConnectionInfo = this._model.SelectedSshConnectionInfo.Select(x => x?.ServiceProvider.GetRequiredService<SshConnectionInfoViewModel>()).ToReadOnlyBindableReactiveProperty();
		this.OpenedFilePath = this._model.OpenedFilePath.ToReadOnlyBindableReactiveProperty();
		this.FileContent = this._model.FileContent.ToReadOnlyBindableReactiveProperty();
		this.ConnectCommand.Subscribe(_ => this._model.Connect());
		this.TestConnectCommand.Subscribe(_ => this._model.TestConnect());
		this.SelectSshConnectionInfoCommand.Subscribe(vm => this._model.SelectedSshConnectionInfo.Value = vm.Model);
		this.AddSavedConnectionsCommand.Subscribe(_ => this._model.AddSavedConnection());
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
