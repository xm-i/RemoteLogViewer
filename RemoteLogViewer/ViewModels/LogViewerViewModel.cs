using RemoteLogViewer.Models.Ssh;
using RemoteLogViewer.ViewModels.Ssh;

namespace RemoteLogViewer.ViewModels;

/// <summary>
///     ログ表示用の ViewModel です。タブタイトルなどの表示情報を提供します。
/// </summary>
[AddScoped]
public class LogViewerViewModel {
	/// <summary>
	///     タブタイトルを取得します。
	/// </summary>
	public string Title {
		get;
		set;
	} = string.Empty;

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
	public BindableReactiveProperty<BaseSshPageViewModel> CurrentPageViewModel {
		get;
	} = new();

	/// <summary>
	///     DI 用コンストラクタ。
	/// </summary>
	public LogViewerViewModel(SshServerSelectorViewModel sshServerSelectorViewModel, SshBrowserViewModel sshBrowserViewModel, SshSessionModel sshSessionModel) {
		this.SshServerSelectorViewModel = sshServerSelectorViewModel;
		this.SshBrowserViewModel = sshBrowserViewModel;
		this.CurrentPageViewModel.Value = this.SshServerSelectorViewModel;

		sshSessionModel.IsConnected.Subscribe(isConnected => {
			if (isConnected) {
				this.CurrentPageViewModel.Value = this.SshBrowserViewModel;
			} else {
				this.CurrentPageViewModel.Value = this.SshServerSelectorViewModel;
			}
		});
	}
}