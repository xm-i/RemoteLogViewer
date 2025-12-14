using System.IO;
using System.Threading;

using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Models.Ssh.FileViewer;
using RemoteLogViewer.Core.Stores.Settings;
using RemoteLogViewer.Core.Utils;
using RemoteLogViewer.Core.Utils.Extensions;

namespace RemoteLogViewer.Core.ViewModels.Ssh.FileViewer;

/// <summary>
///     テキストファイル閲覧 ViewModel。スクロール位置に応じて部分読み込み + GREP 検索を提供します。
/// </summary>
[Inject(InjectServiceLifetime.Scoped)]
public class TextFileViewerViewModel : ViewModelBase<TextFileViewerViewModel> {
	public TextFileViewerModel Model {
		get;
		private set;
	}
	private CancellationTokenSource? _grepCts; // GREP 用 CTS
	private CancellationTokenSource? _saveContentCts; // 指定範囲保存用 CTS

	public TextFileViewerViewModel(TextFileViewerModel textFileViewerModel, SettingsStoreModel settingsStoreModel, ILogger<TextFileViewerViewModel> logger) : base(logger) {
		this.Model = textFileViewerModel;
		// ModelDispose時にVMもDispose
		this.AddTo(this.Model.CompositeDisposable);
		this.OpenedFilePath = this.Model.OpenedFilePath.ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.TotalBytes = this.Model.TotalBytes.Select(x => x is { } ul ? (long?)ul : null).ToReadOnlyBindableReactiveProperty(0).AddTo(this.CompositeDisposable);
		this.FileLoadProgress = this.Model.BuildByteOffsetMapOperation.Progress
			.Throttle()
			.ObserveOnCurrentSynchronizationContext()
			.ToReadOnlyBindableReactiveProperty(0)
			.AddTo(this.CompositeDisposable);
		this.IsFileLoadRunning = this.Model.BuildByteOffsetMapOperation.IsRunning
			.ToReadOnlyBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);
		this.TotalLines = this.Model.TotalLines.Throttle().ObserveOnCurrentSynchronizationContext().ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);

		var grepResultsView = this.Model.GrepResults.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.GrepResults = grepResultsView.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.IsGrepRunning = this.Model.GrepOperation.IsRunning.ToReadOnlyBindableReactiveProperty(false)
			.AddTo(this.CompositeDisposable);
		this.GrepProgress = this.Model.GrepOperation.Progress.ToReadOnlyBindableReactiveProperty(0).AddTo(this.CompositeDisposable);
		var view = this.Model.AvailableEncodings.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.FilteredAvailableEncodings = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);
		this.AvailableEncodings = this.Model.AvailableEncodings.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);

		_ = this.UpdateTotalLineCommand.SubscribeAwait(async (_, ct) => {
			await this.Model.UpdateTotalLines(ct);
		}).AddTo(this.CompositeDisposable);

		_ = this.LoadLogsCommand
			.SubscribeAwait(async (val, ct) => {
				logger.LogTrace($"LoadLogsCommand start: {val.Start} - {val.End}");
				var loaded = await this.Model.GetLinesAsync(val.Start, val.End, ct);
				this._loadedSubject.OnNext((val.RequestId, loaded));
				logger.LogTrace($"LoadLogsCommand end: {val.Start} - {val.End}");
			}, AwaitOperation.Sequential).AddTo(this.CompositeDisposable);

		// GREP 実行: 既存タスクをキャンセルして新規開始
		_ = this.GrepCommand.SubscribeAwait(async (_, ct) => {
			this._grepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			try {
				logger.LogTrace($"Grep start: {this.GrepQuery.Value}");
				await this.Model.Grep(this.GrepQuery.Value, this.GrepStartLine.Value, this.GrepIgnoreCase.Value, this.GrepUseRegex.Value, this._grepCts.Token);
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

		this.SaveRangeProgress = this.Model.SaveRangeOperation.Progress.Throttle().ObserveOnCurrentSynchronizationContext().ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsRangeContentSaving = this.Model.SaveRangeOperation.IsRunning.ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.SaveRangeContentCancelCommand =
			this.IsRangeContentSaving
			.ToReactiveCommand(_ => this._saveContentCts?.Cancel())
			.AddTo(this.CompositeDisposable);

		_ = this.PickupTextLineCommand.SubscribeAwait(async (x, ct) => {
			this.PickedupTextLine.Value = await this.Model.PickupTextLine(x, ct);
		}).AddTo(this.CompositeDisposable);

		_ = this.ChangeEncodingCommand.Subscribe(encoding => {
			this.Model.ChangeEncoding(encoding);
			this._reloadRequestedSubject.OnNext(Unit.Default);
		}).AddTo(this.CompositeDisposable);
	}

	public string PageKey {
		get {
			if (this.Model.PageKey is null) {
				throw new InvalidOperationException();
			}
			return this.Model.PageKey;
		}
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

	public IReadOnlyBindableReactiveProperty<bool> IsFileLoadRunning {
		get;
	}

	/// <summary>総行数。</summary>
	public IReadOnlyBindableReactiveProperty<long> TotalLines {
		get;
	}

	public ReactiveCommand<LogFileLoadRequest> LoadLogsCommand {
		get;
	} = new();

	private readonly Subject<(int RequestId, TextLine[] Content)> _loadedSubject = new();
	public Observable<(int RequestId, TextLine[] Content)> Loaded {
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

	public BindableReactiveProperty<bool> GrepIgnoreCase {
		get;
	} = new(false);
	public BindableReactiveProperty<bool> GrepUseRegex {
		get;
	} = new(false);

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

	public ReactiveCommand<string?> ChangeEncodingCommand {
		get;
	} = new();

	public ReactiveCommand UpdateTotalLineCommand {
		get;
	} = new();

	/// <summary>
	/// 指定範囲のテキストを保存します。
	/// </summary>
	public async Task SaveRangeContent(StreamWriter streamWriter, long startLine, long endLine) {
		this._saveContentCts = new CancellationTokenSource();
		try {
			await this.Model.SaveRangeContent(streamWriter, startLine, endLine, this._saveContentCts.Token);
		} finally {
			this._saveContentCts?.Dispose();
			this._saveContentCts = null;
		}
	}
}
