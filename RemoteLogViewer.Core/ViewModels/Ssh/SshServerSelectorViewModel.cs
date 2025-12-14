using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Models.Ssh;

namespace RemoteLogViewer.Core.ViewModels.Ssh;

/// <summary>
///     SSH 接続設定と接続後の状態管理を行います。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class SshServerSelectorViewModel : BaseSshPageViewModel<SshServerSelectorViewModel> {
	private readonly SshSessionModel _model;

	/// <summary>
	///     接続済みかどうか。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<bool> IsConnected {
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
	/// 利用可能エンコード一覧。
	/// </summary>
	public string[] AvailableEncodings {
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

	public SshServerSelectorViewModel(SshSessionModel model, ILogger<SshServerSelectorViewModel> logger) : base(logger) {
		this._model = model.AddTo(this.CompositeDisposable);
		this.IsConnected = this._model.IsConnected.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		var savedConnectionsView = this._model.SavedConnections.CreateView(x => x.ServiceProvider.GetRequiredService<SshConnectionInfoViewModel>()).AddTo(this.CompositeDisposable);
		this.SavedConnections = savedConnectionsView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.SelectedSshConnectionInfo = this._model.SelectedSshConnectionInfo
			.Select(x => x?.ServiceProvider.GetRequiredService<SshConnectionInfoViewModel>())
			.ToReadOnlyBindableReactiveProperty()
			.AddTo(this.CompositeDisposable);
		this.ConnectCommand.Subscribe(_ => this._model.Connect()).AddTo(this.CompositeDisposable);
		this.TestConnectCommand.Subscribe(_ => this._model.TestConnect()).AddTo(this.CompositeDisposable);
		this.SelectSshConnectionInfoCommand.Subscribe(vm => this._model.SelectedSshConnectionInfo.Value = vm.Model).AddTo(this.CompositeDisposable);
		this.AddSavedConnectionsCommand.Subscribe(_ => this._model.AddSavedConnection()).AddTo(this.CompositeDisposable);
		this.AvailableEncodings = this._model.AvailableEncodings.Select(x => x.Name).ToArray();
	}
}
