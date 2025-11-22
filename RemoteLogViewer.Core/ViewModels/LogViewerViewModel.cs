using System.IO;
using Microsoft.Extensions.Logging;
using RemoteLogViewer.Core.Models.Ssh;
using RemoteLogViewer.Core.Utils;
using RemoteLogViewer.Core.ViewModels.Ssh;

namespace RemoteLogViewer.Core.ViewModels;

/// <summary>
///     ログ表示用の ViewModel です。タブタイトルなどの表示情報を提供します。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class LogViewerViewModel : ViewModelBase<LogViewerViewModel> {
	private readonly SshSessionModel _sshSessionModel;
	/// <summary>
	///     タブタイトルを取得します。
	/// </summary>
	public BindableReactiveProperty<string> Title {
		get;
	} = new("New Log Tab");

	/// <summary>
	///     セッションマネージャ ViewModel への参照。
	/// </summary>
	public SshServerSelectorViewModel SshServerSelectorViewModel {
		get;
	}

	/// <summary>
	/// SSH ブラウザ ViewModel への参照。
	/// </summary>
	public SshBrowserViewModel SshBrowserViewModel {
		get;
	}

	/// <summary>
	/// 現在表示中の Page 用 ViewModel。
	/// </summary>
	public BindableReactiveProperty<IBaseSshPageViewModel> CurrentPageViewModel {
		get;
	} = new();

	/// <summary>
	///     DI 用コンストラクタ。
	/// </summary>
	public LogViewerViewModel(SshServerSelectorViewModel sshServerSelectorViewModel, SshBrowserViewModel sshBrowserViewModel, SshSessionModel sshSessionModel, ILogger<LogViewerViewModel> logger): base(logger) {
		this.SshServerSelectorViewModel = sshServerSelectorViewModel.AddTo(this.CompositeDisposable);
		this.SshBrowserViewModel = sshBrowserViewModel.AddTo(this.CompositeDisposable);
		this.CurrentPageViewModel.Value = this.SshServerSelectorViewModel;
		this._sshSessionModel = sshSessionModel;

		this.SshBrowserViewModel
			.TextFileViewerViewModel
			.OpenedFilePath
			.ObservePropertyChanged(x => x.Value)
			.CombineLatest(
				sshServerSelectorViewModel
					.SelectedSshConnectionInfo
					.ObservePropertyChanged(x => x.Value),
				(filePath, connInfo) => (filePath, connInfo))
			.Subscribe(x => {
			this.Title.Value = $"{Path.GetFileName(x.filePath) ?? string.Empty} @ {x.connInfo?.Name.Value ?? string.Empty}";
		}).AddTo(this.CompositeDisposable);

		sshSessionModel.IsConnected.Subscribe(isConnected => {
			if (isConnected) {
				this.CurrentPageViewModel.Value = this.SshBrowserViewModel;
			} else {
				this.CurrentPageViewModel.Value = this.SshServerSelectorViewModel;
			}
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>
	/// タブを閉じる際に呼び出され、SSH セッションを切断します。
	/// </summary>
	public void Disconnect() {
		this._sshSessionModel.Disconnect();
	}
}