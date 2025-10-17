using System.Text.RegularExpressions;

using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.ViewModels.Ssh.FileViewer;

/// <summary>
///     テキストファイル閲覧 ViewModel。スクロール位置に応じて部分読み込み + GREP 検索を提供します。
/// </summary>
[AddScoped]
public class TextFileViewerViewModel : ViewModelBase {
	private readonly TextFileViewerModel _textFileViewerModel;
	private const long LineHeight = 18;
	public TextFileViewerViewModel(TextFileViewerModel textFileViewerModel) {
		this._textFileViewerModel = textFileViewerModel;
		this.OpenedFilePath = this._textFileViewerModel.OpenedFilePath.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.TotalLines = this._textFileViewerModel.TotalLines.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		var linesView = this._textFileViewerModel.Lines.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.Lines = linesView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.LineNumbers = this.WindowStartLine.CombineLatest(this.VisibleLineCount, (start, count) => Enumerable.Range(0, count).Select(x => start + x).ToArray()).ToReadOnlyBindableReactiveProperty([]).AddTo(this.CompositeDisposable);
		this.TotalHeight = this._textFileViewerModel.TotalLines.Select(x => (x + 1) * LineHeight).ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		var grepResultsView = this._textFileViewerModel.GrepResults.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.GrepResults = grepResultsView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.IsGrepRunning = this._textFileViewerModel.IsGrepRunning.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		var view = this._textFileViewerModel.AvailableEncodings.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.AvailableEncodings = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);

		this.GrepCommand.Subscribe(_ => this._textFileViewerModel.Grep(this.GrepQuery.Value, this.SelectedEncoding.Value)).AddTo(this.CompositeDisposable);
		this.SelectedEncoding.Subscribe(x => {
			if (string.IsNullOrWhiteSpace(x)) {
				view.ResetFilter();
				return;
			}
			view.AttachFilter(ae => Regex.IsMatch(ae, string.Join(".*?", x.Select(c => c)), RegexOptions.IgnoreCase));
		}).AddTo(this.CompositeDisposable);

		this.JumpToLineCommand.Subscribe(line => {
			this.WindowStartLine.Value = Math.Max(1, Math.Min(this.TotalLines.Value - this.VisibleLineCount.Value + 1, line));
		}).AddTo(this.CompositeDisposable);

		this.WindowStartLine
			.CombineLatest(this.VisibleLineCount, (startLine, visibleLineCount) => (startLine, visibleLineCount))
			.CombineLatest(this.OpenedFilePath.ObservePropertyChanged(x => x.Value), (a, path) => (a.startLine, a.visibleLineCount, path))
			.Where(x => x.path != null)
			.ThrottleFirstLast(TimeSpan.FromMilliseconds(100), ObservableSystem.DefaultTimeProvider)
			.Subscribe(x => {
				this._textFileViewerModel.UpdateLines(x.startLine, x.visibleLineCount, this.SelectedEncoding.Value);
			}).AddTo(this.CompositeDisposable);
	}

	/// <summary>開いているファイルのパス。</summary>
	public IReadOnlyBindableReactiveProperty<string?> OpenedFilePath {
		get;
	}
	/// <summary>総行数。</summary>
	public IReadOnlyBindableReactiveProperty<long> TotalLines {
		get;
	}

	/// <summary>総高さ。(px)</summary>
	public IReadOnlyBindableReactiveProperty<long> TotalHeight {
		get;
	}

	/// <summary>現在ウィンドウ開始行。</summary>
	public BindableReactiveProperty<long> WindowStartLine {
		get;
	} = new(1);

	/// <summary>表示行一覧。</summary>
	public NotifyCollectionChangedSynchronizedViewList<TextLine> Lines {
		get;
	}
	/// <summary>行番号一覧。</summary>
	public IReadOnlyBindableReactiveProperty<long[]> LineNumbers {
		get;
	}

	/// <summary>
	/// 画面上表示可能な行数
	/// </summary>
	public BindableReactiveProperty<int> VisibleLineCount {
		get;
	} = new();

	/// <summary>GREP クエリ。</summary>
	public BindableReactiveProperty<string> GrepQuery {
		get;
	} = new();

	/// <summary>GREP 結果。</summary>
	public NotifyCollectionChangedSynchronizedViewList<TextLine> GrepResults {
		get;
	}
	/// <summary>GREP 実行中。</summary>
	public IReadOnlyBindableReactiveProperty<bool> IsGrepRunning {
		get;
	}
	/// <summary>GREP 実行コマンド。</summary>
	public ReactiveCommand GrepCommand {
		get;
	} = new();

	/// <summary>利用可能エンコーディング。</summary>
	public NotifyCollectionChangedSynchronizedViewList<string> AvailableEncodings {
		get;
	}

	/// <summary>選択エンコーディング。</summary>
	public BindableReactiveProperty<string> SelectedEncoding {
		get;
	} = new();

	/// <summary>
	/// GREP 結果行番号ジャンプコマンド。
	/// </summary>
	public ReactiveCommand<long> JumpToLineCommand { get; } = new();

	/// <summary>
	/// 指定範囲のテキストを取得します。
	/// </summary>
	public string? GetRangeContent(long startLine, long endLine) {
		return this._textFileViewerModel.GetRangeContent(startLine, endLine, this.SelectedEncoding.Value);
	}
	/// <summary>
	///     ファイルを開きます。
	/// </summary>
	public void OpenFile(string path, FileSystemObject fso) {
		this._textFileViewerModel.OpenFile(path, fso);
	}
}
