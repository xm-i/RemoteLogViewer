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
}