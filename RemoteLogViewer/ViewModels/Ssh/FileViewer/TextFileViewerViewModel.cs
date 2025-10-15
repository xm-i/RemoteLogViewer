using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.ViewModels.Ssh.FileViewer;

/// <summary>
///     テキストファイル閲覧 ViewModel。スクロール位置に応じて部分読み込み + GREP 検索を提供します。
/// </summary>
[AddScoped]
public class TextFileViewerViewModel {
	private readonly TextFileViewerModel _textFileViewerModel;
	private const long LineHeight = 18;
	public TextFileViewerViewModel(TextFileViewerModel textFileViewerModel) {
		this._textFileViewerModel = textFileViewerModel;
		this.OpenedFilePath = this._textFileViewerModel.OpenedFilePath.ToReadOnlyBindableReactiveProperty();
		this.TotalLines = this._textFileViewerModel.TotalLines.ToReadOnlyBindableReactiveProperty();
		this.Lines = this._textFileViewerModel.Lines.ToNotifyCollectionChanged();
		this.LineNumbers = this.WindowStartLine.CombineLatest(this.VisibleLineCount, (start, count) => Enumerable.Range(1, count).Select(x => start + x).ToArray()).ToReadOnlyBindableReactiveProperty([]);
		this.TotalHeight = this._textFileViewerModel.TotalLines.Select(x => (x + 1) * LineHeight).ToReadOnlyBindableReactiveProperty();
		this.GrepResults = this._textFileViewerModel.GrepResults.ToNotifyCollectionChanged();
		this.IsGrepRunning = this._textFileViewerModel.IsGrepRunning.ToReadOnlyBindableReactiveProperty();
		var view = this._textFileViewerModel.AvailableEncodings.CreateView(x => x);
		this.AvailableEncodings = view.ToNotifyCollectionChanged();

		this.LoadLinesCommand.ThrottleFirstLast(TimeSpan.FromMilliseconds(10), ObservableSystem.DefaultTimeProvider).Subscribe(_ => {
			this._textFileViewerModel.UpdateLines(this.WindowStartLine.Value + 1, this.VisibleLineCount.Value, this.SelectedEncoding.Value);
		});
		this.GrepCommand.Subscribe(_ => this._textFileViewerModel.Grep(this.GrepQuery.Value, this.SelectedEncoding.Value));
		this.SelectedEncoding.Subscribe(x => {
			if (string.IsNullOrWhiteSpace(x)) {
				view.ResetFilter();
				return;
			}
			view.AttachFilter(ae => ae.Contains(x, StringComparison.OrdinalIgnoreCase));
		});

		this.JumpToLineCommand.Subscribe(line => {
			if (line < 1) {
				return;
			}
			// 指定行を先頭に表示したいので WindowStartLine は (line - 1)
			this.WindowStartLine.Value = line - 1;
			// 表示更新
			this.LoadLinesCommand.Execute(Unit.Default);
		});
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
	} = new();

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

	public ReactiveCommand LoadLinesCommand {
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
