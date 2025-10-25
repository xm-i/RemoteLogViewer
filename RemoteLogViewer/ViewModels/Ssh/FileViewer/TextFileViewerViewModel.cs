using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.ViewModels.Ssh.FileViewer;

/// <summary>
///     テキストファイル閲覧 ViewModel。スクロール位置に応じて部分読み込み + GREP 検索を提供します。
/// </summary>
[AddScoped]
public class TextFileViewerViewModel : ViewModelBase {
	private readonly TextFileViewerModel _textFileViewerModel;
	private const long LineHeight = 16;
	private CancellationTokenSource? _grepCts; // GREP 用 CTS
	private CancellationTokenSource? _saveContentCts; // 指定範囲保存用 CTS
	private CancellationTokenSource? _tailCts; // tail-f 用 CTS

	public TextFileViewerViewModel(TextFileViewerModel textFileViewerModel) {
		this._textFileViewerModel = textFileViewerModel;
		this.OpenedFilePath = this._textFileViewerModel.OpenedFilePath.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.FileLoadProgress = this._textFileViewerModel.LoadedBytes
			.CombineLatest(this._textFileViewerModel.TotalBytes, (loaded, total) => total == 0 ? 0d : (double)loaded / total)
			.Throttle()
			.ToReadOnlyBindableReactiveProperty(0)
			.AddTo(this.CompositeDisposable);
		this.TotalLines = this._textFileViewerModel.TotalLines.Throttle().ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		var linesView = this._textFileViewerModel.Lines.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.Lines = linesView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.LineNumbers = this._textFileViewerModel.LineNumbers.ToReadOnlyBindableReactiveProperty([]).AddTo(this.CompositeDisposable);
		this.TotalHeight = this._textFileViewerModel.TotalLines.Select(x => x * LineHeight).ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.ViewerHeight = this.VisibleLineCount.Select(x => x * LineHeight).ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		var grepResultsView = this._textFileViewerModel.GrepResults.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.GrepResults = grepResultsView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.IsGrepRunning = this._textFileViewerModel.GrepOperation.IsRunning.ToReadOnlyBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);
		this.GrepProgress = this._textFileViewerModel.GrepOperation.Progress.ToReadOnlyBindableReactiveProperty(0).AddTo(this.CompositeDisposable);
		var view = this._textFileViewerModel.AvailableEncodings.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.AvailableEncodings = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);

		// Tail 実行状態
		this.IsTailRunning = this._textFileViewerModel.IsTailRunning.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.TailStartCommand =
			this._textFileViewerModel
				.IsTailRunning
				.Select(x => !x)
				.ToReactiveCommand().AddTo(this.CompositeDisposable);
		this.TailStartCommand.SubscribeAwait(async (_, ct) => {
			this._tailCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			try {
				await this._textFileViewerModel.TailFollowAsync(this._tailCts.Token);
			} finally {
				this._tailCts?.Dispose();
				this._tailCts = null;
			}
		});

		this.TailStopCommand = this._textFileViewerModel.IsTailRunning.ToReactiveCommand(_ => {
			this._tailCts?.Cancel();
		}).AddTo(this.CompositeDisposable);

		// 行番号列幅: 桁数に応じて更新
		this.TotalLines.ObservePropertyChanged(x => x.Value).Subscribe(total => {
			var digits = total <= 0 ? 1 : (int)Math.Floor(Math.Log10(total)) + 1;
			if (digits < 2) {
				digits = 2;
			}
			this.LineNumberColumnWidth.Value = (digits * (LineHeight / 2)) + 12; // 余白込み
		}).AddTo(this.CompositeDisposable);

		this.SelectedEncoding.Subscribe(x => {
			if (string.IsNullOrWhiteSpace(x)) {
				view.ResetFilter();
				return;
			}
			view.AttachFilter(ae => Regex.IsMatch(ae, string.Join(".*?", x.Select(c => c)), RegexOptions.IgnoreCase));
		}).AddTo(this.CompositeDisposable);

		this.JumpToLineCommand.Where(x => x != this.WindowStartLine.Value).Subscribe(line => {
			Debug.WriteLine($"Jump to Line Command start: {line}");
			this.WindowStartLine.Value = Math.Max(1, Math.Min(this.TotalLines.Value - this.VisibleLineCount.Value + 1, line));
			Debug.WriteLine("Jump to Line Command end");
		}).AddTo(this.CompositeDisposable);

		this.WindowStartLine
			.CombineLatest(this.VisibleLineCount, (start, count) => (start, count))
			.CombineLatest(
				this.TotalLines
					.ObservePropertyChanged(x => x.Value)
					.Where(x => {
						var count = this.LineNumbers.Value.Count();
						return
							(x < count && this.VisibleLineCount.Value == count) ||
							(x > count && this.VisibleLineCount.Value > count);
					}),
				(x, total) => (x.start, count: (int)Math.Min(x.count, total)))
			.Subscribe(val => {
				Debug.WriteLine("Set LineNumbers start: " + val.start + " - " + (val.start + val.count - 1));
				this._textFileViewerModel.LineNumbers.Value = Enumerable.Range(0, val.count).Select(x => val.start + x).ToArray();
				Debug.WriteLine("Set LineNumbers end: " + val.start + " - " + (val.start + val.count - 1));
			});

		// GREP 実行: 既存タスクをキャンセルして新規開始
		this.GrepCommand.SubscribeAwait(async (_, ct) => {
			this._grepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			try {
				Debug.WriteLine($"Grep start: {this.GrepQuery.Value}");
				await this._textFileViewerModel.Grep(this.GrepQuery.Value, this.SelectedEncoding.Value, this._grepCts.Token);
				Debug.WriteLine("Grep end");
			} finally {
				this._grepCts?.Dispose();
				this._grepCts = null;
			}
		}, AwaitOperation.Switch).AddTo(this.CompositeDisposable);

		// GREP キャンセルコマンド
		this.GrepCancelCommand.Subscribe(_ => {
			this._grepCts?.Cancel();
		}).AddTo(this.CompositeDisposable);

		this.SaveRangeProgress = this._textFileViewerModel.SaveRangeProgress.Throttle().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsRangeContentSaving = this._textFileViewerModel.IsRangeContentSaving.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.SaveRangeContentCancelCommand =
			this.IsRangeContentSaving
			.ToReactiveCommand(_ => this._saveContentCts?.Cancel())
			.AddTo(this.CompositeDisposable);

	}

	/// <summary>開いているファイルのパス。</summary>
	public IReadOnlyBindableReactiveProperty<string?> OpenedFilePath {
		get;
	}

	/// <summary>ファイル読み込み進捗率。</summary>
	public IReadOnlyBindableReactiveProperty<double> FileLoadProgress {
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

	/// <summary>ビュワー高さ。(px)</summary>
	public IReadOnlyBindableReactiveProperty<long> ViewerHeight {
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
	} = new("");

	/// <summary>GREP 結果。</summary>
	public NotifyCollectionChangedSynchronizedViewList<TextLine> GrepResults {
		get;
	}
	/// <summary>GREP 実行中。</summary>
	public IReadOnlyBindableReactiveProperty<bool> IsGrepRunning {
		get;
	}

	public IReadOnlyBindableReactiveProperty<double> GrepProgress {
		get;
	}

	/// <summary>GREP 実行コマンド。</summary>
	public ReactiveCommand GrepCommand {
		get;
	} = new();
	/// <summary>GREP キャンセルコマンド。</summary>
	public ReactiveCommand GrepCancelCommand {
		get;
	} = new();

	/// <summary>利用可能エンコーディング。</summary>
	public NotifyCollectionChangedSynchronizedViewList<string> AvailableEncodings {
		get;
	}

	/// <summary>選択エンコーディング。</summary>
	public BindableReactiveProperty<string?> SelectedEncoding {
		get;
	} = new();

	/// <summary>
	/// GREP 結果行番号ジャンプコマンド。
	/// </summary>
	public ReactiveCommand<long> JumpToLineCommand { get; } = new();

	/// <summary>
	/// 行数列幅。
	/// </summary>
	public BindableReactiveProperty<double> LineNumberColumnWidth { get; } = new(60d);

	public BindableReactiveProperty<TextLine?> SelectedTextLine {
		get;
	} = new();

	/// <summary>
	/// 指定範囲テキスト保存キャンセルコマンド。
	/// </summary>
	public ReactiveCommand SaveRangeContentCancelCommand { get; } = new();

	public BindableReactiveProperty<double> SaveRangeProgress {
		get;
	}

	public BindableReactiveProperty<bool> IsRangeContentSaving {
		get;
	}

	/// <summary>
	/// tail 実行中。
	/// </summary>
	public IReadOnlyBindableReactiveProperty<bool> IsTailRunning {
		get;
	}

	/// <summary>
	/// tail 開始コマンド。
	/// </summary>
	public ReactiveCommand TailStartCommand {
		get;
	}

	/// <summary>
	/// tail 停止コマンド。
	/// </summary>
	public ReactiveCommand TailStopCommand {
		get;
	}

	/// <summary>
	/// 指定範囲のテキストを保存します。
	/// </summary>
	public async Task SaveRangeContent(StreamWriter streamWriter, long startLine, long endLine) {
		this._saveContentCts = new CancellationTokenSource();
		try {
			await this._textFileViewerModel.SaveRangeContent(streamWriter, startLine, endLine, this._saveContentCts.Token);
		} finally {
			this._saveContentCts?.Dispose();
			this._saveContentCts = null;
		}
	}

	/// <summary>
	///     ファイルを開きます。
	/// </summary>
	public async Task OpenFileAsync(string path, FileSystemObject fso, CancellationToken ct) {
		await this._textFileViewerModel.OpenFileAsync(path, fso, this.SelectedEncoding.Value, ct);
	}
}
