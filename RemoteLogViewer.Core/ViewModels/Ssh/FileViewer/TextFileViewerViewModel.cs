using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Models.Ssh.FileViewer;
using RemoteLogViewer.Core.Services.Ssh;
using RemoteLogViewer.Core.Stores.Settings;
using RemoteLogViewer.Core.Utils;
using RemoteLogViewer.Core.Utils.Extensions;

namespace RemoteLogViewer.Core.ViewModels.Ssh.FileViewer;

/// <summary>
///     テキストファイル閲覧 ViewModel。スクロール位置に応じて部分読み込み + GREP 検索を提供します。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class TextFileViewerViewModel : ViewModelBase<TextFileViewerViewModel> {
	private readonly TextFileViewerModel _textFileViewerModel;
	private CancellationTokenSource? _grepCts; // GREP 用 CTS
	private CancellationTokenSource? _saveContentCts; // 指定範囲保存用 CTS
	private CancellationTokenSource? _tailCts; // tail-f 用 CTS

	public TextFileViewerViewModel(TextFileViewerModel textFileViewerModel, SettingsStoreModel settingsStoreModel, ILogger<TextFileViewerViewModel> logger) : base(logger) {
		this._textFileViewerModel = textFileViewerModel;
		this.OpenedFilePath = this._textFileViewerModel.OpenedFilePath.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.TotalBytes = this._textFileViewerModel.TotalBytes.Select(x => x is { } ul ? (long?)ul : null).ToReadOnlyBindableReactiveProperty(0).AddTo(this.CompositeDisposable);
		this.FileLoadProgress = this._textFileViewerModel.BuildByteOffsetMapOperation.Progress
			.Throttle()
			.ObserveOnCurrentSynchronizationContext()
			.ToReadOnlyBindableReactiveProperty(0)
			.AddTo(this.CompositeDisposable);
		this.TotalLines = this._textFileViewerModel.TotalLines.Throttle().ObserveOnCurrentSynchronizationContext().ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);

		var grepResultsView = this._textFileViewerModel.GrepResults.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.GrepResults = grepResultsView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.IsGrepRunning = this._textFileViewerModel.GrepOperation.IsRunning.ToReadOnlyBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);
		this.GrepProgress = this._textFileViewerModel.GrepOperation.Progress.ToReadOnlyBindableReactiveProperty(0).AddTo(this.CompositeDisposable);
		var view = this._textFileViewerModel.AvailableEncodings.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.FilteredAvailableEncodings = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.AvailableEncodings = this._textFileViewerModel.AvailableEncodings.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);

		// Tail 実行状態
		this.IsTailRunning = this._textFileViewerModel.TailOperation.IsRunning.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.TailStartCommand =
			this._textFileViewerModel
				.TailOperation
				.IsRunning
				.Select(x => !x)
				.ToReactiveCommand().AddTo(this.CompositeDisposable);
		_ = this.TailStartCommand.SubscribeAwait(async (_, ct) => {
			this._tailCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			try {
				await this._textFileViewerModel.TailFollowAsync(this._tailCts.Token);
			} finally {
				this._tailCts?.Dispose();
				this._tailCts = null;
			}
		});

		this.TailStopCommand = this._textFileViewerModel.TailOperation.IsRunning.ToReactiveCommand(_ => {
			this._tailCts?.Cancel();
		}).AddTo(this.CompositeDisposable);

		_ = this.SelectedEncoding.Subscribe(x => {
			if (string.IsNullOrWhiteSpace(x)) {
				view.ResetFilter();
				return;
			}
			view.AttachFilter(ae => Regex.IsMatch(ae, string.Join(".*?", x.Select(c => c)), RegexOptions.IgnoreCase));
		}).AddTo(this.CompositeDisposable);

		_ = this.LoadLogsCommand
			.SubscribeAwait(async (val, ct) => {
				logger.LogTrace($"LoadLogsCommand start: {val.Start} - {val.End}");
				var loaded = await this._textFileViewerModel.GetLinesAsync(val.Start, val.End, ct);
				this._loadedSubject.OnNext(loaded);
				logger.LogTrace($"LoadLogsCommand end: {val.Start} - {val.End}");
			}, AwaitOperation.Switch).AddTo(this.CompositeDisposable);

		// GREP 実行: 既存タスクをキャンセルして新規開始
		_ = this.GrepCommand.SubscribeAwait(async (_, ct) => {
			this._grepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			try {
				logger.LogTrace($"Grep start: {this.GrepQuery.Value}");
				await this._textFileViewerModel.Grep(this.GrepQuery.Value, this.SelectedEncoding.Value, this.GrepStartLine.Value, this._grepCts.Token);
				logger.LogTrace("Grep end");
			} finally {
				this._grepCts?.Dispose();
				this._grepCts = null;
				this.GrepStartLine.Value = this.GrepResults.Max(x => x.LineNumber) + 1;
			}
		}, AwaitOperation.Switch).AddTo(this.CompositeDisposable);

		// GREP キャンセルコマンド
		_ = this.GrepCancelCommand.Subscribe(_ => {
			this._grepCts?.Cancel();
		}).AddTo(this.CompositeDisposable);

		this.SaveRangeProgress = this._textFileViewerModel.SaveRangeOperation.Progress.Throttle().ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsRangeContentSaving = this._textFileViewerModel.SaveRangeOperation.IsRunning.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.SaveRangeContentCancelCommand =
			this.IsRangeContentSaving
			.ToReactiveCommand(_ => this._saveContentCts?.Cancel())
			.AddTo(this.CompositeDisposable);

		_ = this.PickupTextLineCommand.SubscribeAwait(async (x, ct) => {
			this.PickedupTextLine.Value = await this._textFileViewerModel.PickupTextLine(x, ct);
		}).AddTo(this.CompositeDisposable);

		_ = this.ChangeEncodingCommand.Subscribe(_ => {
			if (this.SelectedEncoding.Value is null) {
				return;
			}
			this._textFileViewerModel.ChangeEncoding(this.SelectedEncoding.Value);
			this._reloadRequestedSubject.OnNext(Unit.Default);
		}).AddTo(this.CompositeDisposable);
	}

	/// <summary>開いているファイルのパス。</summary>
	public IReadOnlyBindableReactiveProperty<string?> OpenedFilePath {
		get;
	}

	/// <summary>総バイト数。</summary>
	public IReadOnlyBindableReactiveProperty<long?> TotalBytes {
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

	public ReactiveCommand<LogFileRange> LoadLogsCommand {
		get;
	} = new();

	private readonly Subject<TextLine[]> _loadedSubject = new();
	public Observable<TextLine[]> Loaded {
		get {
			return field ??= this._loadedSubject.AsObservable();
		}
	}

	private readonly Subject<Unit> _reloadRequestedSubject = new();
	public Observable<Unit> ReloadRequested {
		get {
			return field ??= this._reloadRequestedSubject.AsObservable();
		}
	}

	/// <summary>GREP クエリ。</summary>
	public BindableReactiveProperty<string> GrepQuery {
		get;
	} = new("");

	/// <summary>GREP 開始行。</summary>
	public BindableReactiveProperty<long> GrepStartLine {
		get;
	} = new(1);

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

	/// <summary>フィルター後利用可能エンコーディング。</summary>
	public NotifyCollectionChangedSynchronizedViewList<string> FilteredAvailableEncodings {
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


	public ReactiveCommand<long> PickupTextLineCommand {
		get;
	} = new();

	public BindableReactiveProperty<TextLine?> PickedupTextLine {
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

	public ReactiveCommand ChangeEncodingCommand {
		get;
	} = new();

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

	/// <summary>
	/// 表示削減量算出
	/// </summary>
	/// <param name="contents">コンテンツ</param>
	/// <param name="totalDecrement">合計削減量</param>
	/// <returns></returns>
	private static Dictionary<int, int> CalcDecrement(IEnumerable<string> contents, int totalDecrement) {
		var n = contents.Count();
		// 元の値とインデックスをペアで保持
		var indexedNums = contents
			.Select((x, Index) => new { x.Length, Index })
			.OrderByDescending(x => x.Length)
			.ToList();

		var reduce = new int[n]; // ソート後の減らす量
		var remain = totalDecrement;

		for (var i = 0; i < n && remain > 0; i++) {
			var nextLength = (i < n - 1) ? indexedNums[i + 1].Length : 0;
			var diff = indexedNums[i].Length - nextLength;
			var canReduce = diff * (i + 1);

			var use = Math.Min(remain, canReduce);
			var lengthPerIndex = use / (i + 1);
			var extra = use % (i + 1);

			for (var j = 0; j <= i; j++) {
				reduce[j] += lengthPerIndex;
				if (extra > 0) {
					reduce[j]++;
					extra--;
				}
			}
			remain -= use;
		}

		// 結果を元のINDEXに戻す
		var result = new Dictionary<int, int>();
		for (var i = 0; i < n; i++) {
			var originalIndex = indexedNums[i].Index;
			if (reduce[i] > 0) {
				result[originalIndex] = reduce[i];
			}
		}
		return result;
	}
}
