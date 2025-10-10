using RemoteLogViewer.ViewModels.Ssh;

namespace RemoteLogViewer.ViewModels;

/// <summary>
///     ログ表示用の ViewModel です。タブタイトルなどの表示情報を提供します。
/// </summary>
[AddTransient]
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
	public SshSessionViewModel SshSessionViewModel {
		get;
	}

	/// <summary>
	///     DI 用コンストラクタ。
	/// </summary>
	public LogViewerViewModel(SshSessionViewModel sshSessionViewModel) {
		this.SshSessionViewModel = sshSessionViewModel;
	}
}