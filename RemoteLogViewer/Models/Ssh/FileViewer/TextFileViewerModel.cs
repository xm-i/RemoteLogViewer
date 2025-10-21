using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Threading;

using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.Models.Ssh.FileViewer;

[AddScoped]
public class TextFileViewerModel : ModelBase {
	private readonly SshService _sshService;
	private const double loadingBuffer = 5;
	private readonly Lock _syncObj = new();
	private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokenSources = [];
	private readonly List<ByteOffset> _byteOffsetMap = [];

	public TextFileViewerModel(SshService sshService) {
		this._sshService = sshService;

		var lineNumbersChangedStream = this.LineNumbers
			.CombineLatest(this.OpenedFilePath, (lineNumbers, path) => (lineNumbers, path))
			.Throttle()
			.Where(x => x.path != null);

		// 表示行枠確保
		lineNumbersChangedStream.Subscribe(x => {
			Debug.WriteLine($"lineNumbersChanged start:{x.lineNumbers.FirstOrDefault()}");
			lock (this._syncObj) {
				this.Lines.Clear();
				this.Lines.AddRange(x.lineNumbers.Select((num, i) => this.LoadedLines.TryGetValue(num, out var val) ? val : new TextLine(num, "", false)));
			}
			Debug.WriteLine("lineNumbersChanged end");
		});

		// 表示行データ取得
		lineNumbersChangedStream
			.Where(x => x.lineNumbers.Length > 0)
			.ThrottleFirstLast(TimeSpan.FromMilliseconds(100), ObservableSystem.DefaultTimeProvider)
			.SubscribeAwait(async (x, ct) => {
				Debug.WriteLine($"PreLoadLines start:{x.lineNumbers.Min()},{x.lineNumbers.Length}");
				await this.PreLoadLinesAsync(x.lineNumbers.Min(), x.lineNumbers.Length, ct);
				Debug.WriteLine($"PreLoadLines end");
			}, AwaitOperation.Switch).AddTo(this.CompositeDisposable);

		// 表示行内容更新
		this.LoadedLines
			.ObserveChanged()
			.Where(x => x.Action == NotifyCollectionChangedAction.Add || x.Action == NotifyCollectionChangedAction.Replace)
			.Subscribe(x => {
				Debug.WriteLine($"loadedLines updated start:{x.NewItem.Value.LineNumber}");
				lock (this._syncObj) {
					foreach (var (tl, i) in this.Lines.Select((tl, i) => (tl, i)).ToArray()) {
						if (tl.LineNumber != x.NewItem.Value.LineNumber) {
							continue;
						}
						this.Lines[i] = x.NewItem.Value;
					}
				}
				Debug.WriteLine($"loadedLines updated end");
			});
	}

	public ReactiveProperty<long[]> LineNumbers {
		get;
	} = new([]);

	/// <summary>開いているファイルのフルパス。</summary>
	public ReactiveProperty<string?> OpenedFilePath {
		get;
	} = new(null);

	/// <summary>総行数。</summary>
	public ReactiveProperty<long> TotalLines {
		get;
	} = new();

	/// <summary>
	/// 表示行。
	/// </summary>
	public ObservableList<TextLine> Lines {
		get;
	} = new();

	/// <summary>GREP 結果行。</summary>
	public ObservableList<TextLine> GrepResults {
		get;
	} = new();

	/// <summary>GREP 実行中か。</summary>
	public ReactiveProperty<bool> IsGrepRunning {
		get;
	} = new(false);

	/// <summary>利用可能エンコーディング。</summary>
	public ObservableList<string> AvailableEncodings { get; } = [];

	/// <summary>
	/// 選択中エンコーディング
	/// </summary>
	public ReactiveProperty<string?> FileEncoding { get; } = new(null);

	/// <summary>
	/// 読み込み済みテキスト
	/// </summary>
	public ObservableDictionary<long, TextLine> LoadedLines {
		get;
	} = [];

	/// <summary>
	/// バイトオフセット作成済みサイズ
	/// </summary>
	public ReactiveProperty<ulong> LoadedBytes {
		get;
	} = new(0);

	/// <summary>
	/// ファイルサイズ
	/// </summary>
	public ReactiveProperty<ulong> TotalBytes {
		get;
	} = new(0);

	/// <summary>
	///     ファイルを開き内容を取得します。
	/// </summary>
	/// <param name="path">パス。</param>
	/// <param name="fso">ファイル。</param>
	/// <param name="encoding">エンコード</param>
	/// <param name="ct">キャンセルトークン。</param>
	public async Task OpenFileAsync(string path, FileSystemObject fso, string? encoding, CancellationToken ct) {
		if (fso.FileSystemObjectType is not (FileSystemObjectType.File or FileSystemObjectType.Symlink)) {
			return;
		}
		this.ResetStates();
		var fullPath = PathUtils.CombineUnixPath(path, fso.FileName, fso.FileSystemObjectType);
		var escaped = fullPath.Replace("\"", "\\\"");

		var guid = Guid.NewGuid();
		var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		this._cancellationTokenSources.TryAdd(guid, linkedCts);
		try {
			this.OpenedFilePath.Value = fullPath;
			this.FileEncoding.Value = encoding;
			this.TotalBytes.Value = fso.FileSize;
			// 非同期でバイトオフセットマップ作成
			if (this.OpenedFilePath.Value != null) {
				this._byteOffsetMap.Clear();
				try {
					await foreach (var entry in this._sshService.CreateByteOffsetMap(fullPath, 10000, linkedCts.Token)) {
						this._byteOffsetMap.Add(entry);
						this.TotalLines.Value = entry.LineNumber;
						this.LoadedBytes.Value = entry.Bytes;
					}
				} catch {
					// TODO: エラー処理
				}
			}
		} finally {
			this._cancellationTokenSources.TryRemove(guid, out _);
		}
	}

	/// <summary>
	/// テキストファイル参照に利用可能なエンコードを取得します。
	/// </summary>
	public void LoadAvailableEncoding() {
		this.AvailableEncodings.Clear();
		this.AvailableEncodings.AddRange(this._sshService.ListIconvEncodings());
	}

	/// <summary>
	/// 指定された範囲のテキストを読み込みます。
	/// 指定範囲に対して、テキストを事前に多めに読み込んでおき、読み込み量が少ない場合は読み込み処理をスキップします。
	/// </summary>
	/// <param name="startLine">開始行</param>
	/// <param name="visibleCount">表示可能行</param>
	/// <param name="ct">キャンセルトークン</param>
	private async Task PreLoadLinesAsync(long startLine, long visibleCount, CancellationToken ct) {
		var guid = Guid.NewGuid();
		var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		this._cancellationTokenSources.TryAdd(guid, linkedCts);

		try {
			if (this.OpenedFilePath.Value == null) {
				throw new InvalidOperationException();
			}

			// 読み込み行設定
			var loadStartLine = Math.Max(1, startLine - (int)(visibleCount * loadingBuffer));
			var loadEndLine = startLine + (int)(visibleCount * loadingBuffer);

			// 読み込み済み範囲は除外 (開始行)
			for (var i = loadStartLine; i <= loadEndLine; i++) {
				if (this.LoadedLines.ContainsKey(i)) {
					if (i == loadStartLine) {
						loadStartLine++;
					}
				}
			}

			// 読み込み済み範囲は除外 (終了行)
			for (var i = loadEndLine; i >= loadStartLine; i--) {
				if (this.LoadedLines.ContainsKey(i)) {
					if (i == loadEndLine) {
						loadEndLine--;
					}
				}
			}

			if (
				loadEndLine < loadStartLine ||
				((
					loadStartLine > startLine + visibleCount ||
					loadEndLine < startLine
				) &&
				loadEndLine - loadStartLine < visibleCount)) {
				// 読み込み対象行がないか、読み込み対象が表示範囲外かつ、読み込み対象が少ない場合は読み込みスキップ
				return;
			}

			var byteOffset = this.FindOffset(loadStartLine);
			var lines = this._sshService.GetLinesAsync(this.OpenedFilePath.Value, loadStartLine, loadEndLine, this.FileEncoding.Value, byteOffset, linkedCts.Token);

			await foreach (var line in lines) {
				this.LoadedLines[line.LineNumber] = line;
			}
		} finally {
			this._cancellationTokenSources.TryRemove(guid, out _);
		}
	}

	/// <summary>
	/// GREP 実行。クエリが空の場合は結果をクリア。
	/// </summary>
	public async Task Grep(string? query, string? encoding, CancellationToken ct) {
		var guid = Guid.NewGuid();
		var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		this._cancellationTokenSources.TryAdd(guid, linkedCts);
		if (this.OpenedFilePath.Value == null) {
			return;
		}
		if ((query?.Length ?? 0) == 0) {
			return;
		}

		this.GrepResults.Clear();
		try {
			this.IsGrepRunning.Value = true;
			var lines = this._sshService.GrepAsync(this.OpenedFilePath.Value, query!, false, encoding, linkedCts.Token);
			await foreach (var line in lines) {
				this.GrepResults.Add(line);
			}
		} finally {
			this.IsGrepRunning.Value = false;
			this._cancellationTokenSources.TryRemove(guid, out _);
		}
	}

	/// <summary>
	/// ファイルを閉じます。
	/// </summary>
	public void CloseFile() {
		this.OpenedFilePath.Value = null;
		this.ResetStates();
	}

	/// <summary>
	/// 指定行範囲のテキスト内容を取得します。
	/// </summary>
	/// <param name="startLine">開始行 (1 始まり)</param>
	/// <param name="endLine">終了行 (1 始まり)</param>
	/// <param name="encoding">ソースエンコーディング</param>
	/// <param name="ct">キャンセルトークン</param>
	/// <returns>結合済みテキスト (末尾改行無し)</returns>
	public async IAsyncEnumerable<string> GetRangeContent(long startLine, long endLine, [EnumeratorCancellation] CancellationToken ct) {
		var guid = Guid.NewGuid();
		var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		this._cancellationTokenSources.TryAdd(guid, linkedCts);
		try {
			if (this.OpenedFilePath.Value == null) {
				yield break;
			}
			if (startLine < 1 || endLine < startLine) {
				yield break;
			}
			endLine = Math.Min(endLine, this.TotalLines.Value);
			var byteOffset = this.FindOffset(startLine);
			var lines = this._sshService.GetLinesAsync(this.OpenedFilePath.Value, startLine, endLine, this.FileEncoding.Value, byteOffset, linkedCts.Token);
			await foreach (var line in lines.Select(l => l.Content!)) {
				yield return line;
			}
		} finally {
			this._cancellationTokenSources.TryRemove(guid, out _);
		}
	}

	/// <summary>
	/// リセット
	/// </summary>
	private void ResetStates() {
		foreach (var cts in this._cancellationTokenSources.Values) {
			try {
				cts.Cancel();
			} catch {
				// ignore
			}
		}
		this.TotalLines.Value = 0;
		this.OpenedFilePath.Value = null;
		this.GrepResults.Clear();
		this.LoadedLines.Clear();
		this.Lines.Clear();
		this._byteOffsetMap.Clear();
	}

	private ByteOffset FindOffset(long targetLine) {
		ByteOffset result = new(0, 0);
		foreach (var bo in this._byteOffsetMap) {
			if (bo.LineNumber < targetLine) {
				result = bo;
			} else {
				break; // assuming map sorted ascending
			}
		}
		return result;
	}
}
