using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Models.Ssh.FileViewer.ByteOffsetMap;
using RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;
using RemoteLogViewer.Core.Services.Ssh;
using RemoteLogViewer.Core.Stores.Settings;
using RemoteLogViewer.Core.Utils;
using RemoteLogViewer.Core.Utils.Extensions;

namespace RemoteLogViewer.Core.Models.Ssh.FileViewer;

[Inject(InjectServiceLifetime.Scoped)]
public class TextFileViewerModel : ModelBase<TextFileViewerModel> {
	private ISshService? _sshService;
	private readonly SettingsStoreModel _settingsStore;
	private readonly IOperationRegistry _operations;
	private readonly IByteOffsetIndex _byteOffsetIndex;

	public TextFileViewerModel(
		SettingsStoreModel settingsStore,
		IOperationRegistry operations,
		IByteOffsetIndex byteOffsetIndex,
		IGrepOperation grepOperation,
		ISaveRangeContentOperation saveRangeContentOperation,
		IBuildByteOffsetMapOperation buildByteOffsetMapOperation,
		IServiceProvider serviceProvider,
		ILogger<TextFileViewerModel> logger) : base(logger) {
		this._settingsStore = settingsStore;
		this._operations = operations.AddTo(this.CompositeDisposable);
		this._byteOffsetIndex = byteOffsetIndex;
		this.GrepOperation = grepOperation.AddTo(this.CompositeDisposable);
		this.ServiceProvider = serviceProvider;
		_ = this.TotalLines.Subscribe(x => {
			this.GrepOperation.TotalLineCount.Value = x;
		}).AddTo(this.CompositeDisposable);
		this.SaveRangeOperation = saveRangeContentOperation.AddTo(this.CompositeDisposable);
		this.BuildByteOffsetMapOperation = buildByteOffsetMapOperation.AddTo(this.CompositeDisposable);
	}

	public string? PageKey {
		get;
		private set;
	}

	public IServiceProvider ServiceProvider {
		get;
	}

	/// <summary>開いているファイルのフルパス。</summary>
	public ReactiveProperty<string?> OpenedFilePath {
		get;
	} = new(null);

	/// <summary>総行数。</summary>
	public ReactiveProperty<long> TotalLines {
		get;
	} = new();

	/// <summary>GREP 結果行。</summary>
	public ObservableList<TextLine> GrepResults {
		get;
	} = [];

	public IGrepOperation GrepOperation {
		get;
	}

	public ISaveRangeContentOperation SaveRangeOperation {
		get;
	}
	public IBuildByteOffsetMapOperation BuildByteOffsetMapOperation {
		get;
	}

	/// <summary>利用可能エンコーディング。</summary>
	public ObservableList<string> AvailableEncodings { get; } = [];

	/// <summary>
	/// 選択中エンコーディング
	/// </summary>
	public ReactiveProperty<string?> FileEncoding { get; } = new(null);

	/// ファイルサイズ
	/// </summary>
	public ReactiveProperty<ulong?> TotalBytes {
		get;
	} = new(0);

	[MemberNotNull(nameof(_sshService))]
	[MemberNotNull(nameof(PageKey))]
	public void Initialize(ISshService sshService, string pageKey) {
		this._sshService = sshService;
		this.PageKey = pageKey;
	}

	/// <summary>
	///     ファイルを開き内容を取得します。
	/// </summary>
	/// <param name="path">パス。</param>
	/// <param name="fso">ファイル。</param>
	/// <param name="encoding">エンコード</param>
	/// <param name="ct">キャンセルトークン。</param>
	public async Task OpenFileAsync(string path, FileSystemObject fso, CancellationToken ct) {
		if (this._sshService is null) {
			throw new InvalidOperationException();
		}
		if (fso.FileSystemObjectType is not (FileSystemObjectType.File or FileSystemObjectType.SymlinkFile)) {
			return;
		}
		this.ResetStates();
		var fullPath = PathUtils.CombineUnixPath(path, fso.FileName, fso.FileSystemObjectType);
		this.OpenedFilePath.Value = fullPath;
		this.TotalBytes.Value = fso.FileSize;
		this._byteOffsetIndex.Reset();
		var mapStream = this.BuildByteOffsetMapOperation.RunAsync(this._sshService, fullPath, this._settingsStore.SettingsModel.AdvancedSettings.ByteOffsetMapChunkSize.Value, fso.FileSize, null, ct);
		await foreach (var entry in mapStream.Select(entry => new ByteOffset(entry.LineNumber, entry.Bytes)).ChunkForAddRange(TimeSpan.FromMilliseconds(300), null, ct)) {
			this._byteOffsetIndex.AddRange(entry);
			this.TotalLines.Value = entry.Max(x => x.LineNumber - 1);
		}
	}

	/// <summary>
	/// テキストファイル参照に利用可能なエンコードを取得します。
	/// </summary>
	public void LoadAvailableEncoding() {
		if (this._sshService is null) {
			throw new InvalidOperationException();
		}
		this.AvailableEncodings.Clear();
		this.AvailableEncodings.AddRange(this._sshService.ListIconvEncodings());
	}

	/// <summary>
	/// 指定された範囲のテキストを取得します。
	/// </summary>
	/// <param name="startLine">開始行</param>
	/// <param name="endLine">終了行</param>
	/// <param name="ct">キャンセルトークン</param>
	/// <returns>取得した行群</returns>
	public async Task<TextLine[]> GetLinesAsync(long startLine, long endLine, CancellationToken ct) {
		if (this._sshService is null) {
			throw new InvalidOperationException();
		}
		using var op = this._operations.Register(ct);
		try {
			if (this.OpenedFilePath.Value == null) {
				throw new InvalidOperationException();
			}
			var byteOffset = this._byteOffsetIndex.Find(startLine);
			var lines = this._sshService.GetLinesAsync(this.OpenedFilePath.Value, startLine, endLine, this.FileEncoding.Value, byteOffset, op.Token);
			return await lines.ToArrayAsync();
		} finally { }
	}

	/// <summary>
	/// GREP 実行。クエリが空の場合は結果をクリア。
	/// </summary>
	public async Task Grep(string? query, string? encoding, long startLine, bool ignoreCase, bool useRegex, CancellationToken ct) {
		if (this._sshService is null) {
			throw new InvalidOperationException();
		}
		this.GrepResults.Clear();
		var max = this._settingsStore.SettingsModel.TextViewerSettings.GrepMaxResults.Value;
		var startOffset = this._byteOffsetIndex.Find(startLine);
		await foreach (var lines in this.GrepOperation.RunAsync(this._sshService, this.OpenedFilePath.Value, query, encoding, startOffset, startLine, max, ignoreCase, useRegex, ct).ChunkForAddRange(TimeSpan.FromMilliseconds(500), null, ct)) {
			this.GrepResults.AddRange(lines);
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
	/// tail -f によりファイル末尾の追記を監視し、新規行番号を TotalLines に反映します。
	///監視開始時点の末尾行の次の行から取得します。
	/// </summary>
	/// <param name="ct">キャンセルトークン。</param>
	public async Task UpdateTotalLines(CancellationToken ct) {
		if (this._sshService is null) {
			throw new InvalidOperationException();
		}
		if (this.OpenedFilePath.Value == null) {
			return;
		}
		if (this.BuildByteOffsetMapOperation.IsRunning.CurrentValue) {
			// バイトオフセットマップ作成中は待機
			_ = await this.BuildByteOffsetMapOperation.IsRunning.Where(x => !x).FirstAsync(ct);
		}

		var lsResult = this._sshService.ListDirectory(this.OpenedFilePath.Value);

		if (lsResult.Length != 1) {
			throw new InvalidOperationException();
		}

		this.TotalBytes.Value = lsResult[0].FileSize;

		var mapStream = this.BuildByteOffsetMapOperation.RunAsync(this._sshService, this.OpenedFilePath.Value, this._settingsStore.SettingsModel.AdvancedSettings.ByteOffsetMapChunkSize.Value, lsResult[0].FileSize, this._byteOffsetIndex.FindLast(), ct);
		await foreach (var entry in mapStream.Select(entry => new ByteOffset(entry.LineNumber, entry.Bytes)).ChunkForAddRange(TimeSpan.FromMilliseconds(300), null, ct)) {
			this._byteOffsetIndex.AddRange(entry);
			this.TotalLines.Value = entry.Max(x => x.LineNumber - 1);
		}
	}

	/// <summary>
	/// 指定行範囲のテキスト内容を保存します。
	/// </summary>
	/// <param name="startLine">開始行 (1 始まり)</param>
	/// <param name="endLine">終了行 (1 始まり)</param>
	/// <param name="ct">キャンセルトークン</param>
	/// <returns>結合済みテキスト (末尾改行無し)</returns>
	public async Task SaveRangeContent(StreamWriter streamWriter, long startLine, long endLine, CancellationToken ct) {
		if (this._sshService is null) {
			throw new InvalidOperationException();
		}
		await this.SaveRangeOperation.ExecuteAsync(this._sshService, this.OpenedFilePath.Value, streamWriter, startLine, endLine, this.FileEncoding.Value, ct);
	}

	public async Task<TextLine?> PickupTextLine(long lineNumber, CancellationToken ct) {
		if (this._sshService is null) {
			throw new InvalidOperationException();
		}
		if (this.OpenedFilePath.Value == null) {
			return null;
		}
		var byteOffset = this._byteOffsetIndex.Find(lineNumber);
		var lines = await this._sshService.GetLinesAsync(this.OpenedFilePath.Value, lineNumber, lineNumber, this.FileEncoding.Value, byteOffset, ct).ToArrayAsync();

		return lines.FirstOrDefault();
	}

	public void ChangeEncoding(string? encoding) {
		this.FileEncoding.Value = encoding;
		this._operations.CancelAll();
		this.GrepResults.Clear();
	}

	/// <summary>
	/// リセット
	/// </summary>
	private void ResetStates() {
		this._operations.CancelAll();
		this.TotalLines.Value = 0;
		this.TotalBytes.Value = null;
		this.OpenedFilePath.Value = null;
		this.GrepResults.Clear();
		this._byteOffsetIndex.Reset();
		this.SaveRangeOperation.Reset();
		this.BuildByteOffsetMapOperation.Reset();
	}
}
