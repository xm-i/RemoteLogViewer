namespace RemoteLogViewer.ViewModels;

/// <summary>
///     ログ表示用の ViewModel です。タブタイトルなどの表示情報を提供します。
/// </summary>
public class LogViewerViewModel {
	/// <summary>
	///     タブタイトルを取得します。
	/// </summary>
	public string Title {
		get;
	}

	/// <summary>
	///     <see cref="LogViewerViewModel"/> の新しいインスタンスを初期化します。
	/// </summary>
	/// <param name="title">タブタイトル。</param>
	public LogViewerViewModel(string title) {
		this.Title = title;
	}
}