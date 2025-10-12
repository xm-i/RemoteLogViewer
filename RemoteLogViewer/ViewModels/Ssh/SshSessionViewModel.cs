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
	/// ディレクトリ移動コマンド。
	/// </summary>
	public ReactiveCommand<FileSystemObject> EnterDirectoryCommand { get; } = new();

	/// <summary>
	/// パスナビゲートコマンド (テキストボックス編集適用)。
	/// </summary>
	public ReactiveCommand NavigatePathCommand { get; } = new();

	public SshSessionViewModel(SshSessionModel model) {
		this._model = model;
		this.IsConnected = this._model.IsConnected.ToReadOnlyBindableReactiveProperty();
		this.CurrentPath = this._model.CurrentPath!.ToBindableReactiveProperty()!;
		this.Entries = this._model.Entries.ToNotifyCollectionChanged();
		this.SavedConnections = this._model.SavedConnections.ToNotifyCollectionChanged(x => x.ServiceProvider.GetRequiredService<SshConnectionInfoViewModel>());
		this.SelectedSshConnectionInfo = this._model.SelectedSshConnectionInfo.Select(x => x?.ServiceProvider.GetRequiredService<SshConnectionInfoViewModel>()).ToReadOnlyBindableReactiveProperty();
		this.ConnectCommand.Subscribe(_ => this._model.Connect());
		this.TestConnectCommand.Subscribe(_ => this._model.TestConnect());
		this.SelectSshConnectionInfoCommand.Subscribe(vm => this._model.SelectedSshConnectionInfo.Value = vm.Model);
		this.AddSavedConnectionsCommand.Subscribe(_ => this._model.AddSavedConnection());
		this.DisconnectCommand.Subscribe(_ => this._model.Disconnect());
		this.EnterDirectoryCommand.Subscribe(fso => {
			if (fso?.FileSystemObjectType == FileSystemObjectType.Directory) {
				this._model.EnterDirectory(fso.FileName);
			}
		});
		this.NavigatePathCommand.Subscribe(_ => {
			this._model.NavigateTo(this.CurrentPath.Value);
		});
	}
}
