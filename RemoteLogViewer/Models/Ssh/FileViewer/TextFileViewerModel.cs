using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Utils;
using RemoteLogViewer.Utils.Extensions;

namespace RemoteLogViewer.Models.Ssh.FileViewer;

[AddScoped]
public class TextFileViewerModel {
	private readonly SshService _sshService;
	private const double loadingBuffer = 5;

	public TextFileViewerModel(SshService sshService) {
		this._sshService = sshService;
	}

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
	/// 読み込み済みテキスト
	/// </summary>
	public ObservableDictionary<long, TextLine> LoadedLines {
		get;
	} = [];

	/// <summary>
	///     ファイルを開き内容を取得します。
	/// </summary>
	/// <param name="path">パス。</param>
	/// <param name="fso">ファイル。</param>
	public void OpenFile(string path, FileSystemObject fso) {
		if (fso.FileSystemObjectType is not (FileSystemObjectType.File or FileSystemObjectType.Symlink)) {
			return;
		}
		this.ResetStates();
		var fullPath = PathUtils.CombineUnixPath(path, fso.FileName);
		var escaped = fullPath.Replace("\"", "\\\"");
		try {
			this.TotalLines.Value = this._sshService.GetLineCount(fullPath);
			this.OpenedFilePath.Value = fullPath;
		} catch {
			this.OpenedFilePath.Value = fullPath;
		}
	}

	/// <summary>
	/// テキストファイル参照に利用可能なエンコードを取得します。
	/// </summary>
	public void LoadAvailableEncoding() {
		this.AvailableEncodings.AddRange(this._sshService.ListIconvEncodings());
	}

	/// <summary>
	/// 指定行範囲のテキストを表示中に設定します。
	/// パフォーマンス向上のため、事前に多めに読み込みしておく。
	/// </summary>
	/// <param name="startLine">開始行</param>
	/// <param name="visibleCount">表示可能行</param>
	/// <param name="encoding">エンコード</param>
	public void UpdateLines(long startLine, long visibleCount, string encoding) {
		this.PreLoadLines(startLine, visibleCount, encoding);
		foreach (var line in this.Lines.ToArray()) {
			if (startLine <= line.LineNumber && line.LineNumber < startLine + visibleCount) {
				// 表示範囲に含まれる行は残す
				continue;
			}
			this.Lines.Remove(line);
		}
		for (var i = startLine; i < startLine + visibleCount; i++) {
			// 表示範囲に含まれる行を追加
			if (this.LoadedLines.ContainsKey(i)) {
				if (!this.Lines.Any(x => x.LineNumber == i)) {
					this.Lines.Insert((int)(i - startLine), this.LoadedLines[i]);
				}
			}
		}
	}

	/// <summary>
	/// 指定された範囲のテキストを読み込みます。
	/// 指定範囲に対して、テキストを事前に多めに読み込んでおき、読み込み量が少ない場合は読み込み処理をスキップします。
	/// </summary>
	/// <param name="startLine">開始行</param>
	/// <param name="visibleCount">表示可能行</param>
	/// <param name="encoding">エンコード</param>
	public void PreLoadLines(long startLine, long visibleCount, string encoding) {
		if (this.OpenedFilePath.Value == null) {
			throw new InvalidOperationException();
		}

		// 読み込み行設定
		var loadStartLine = Math.Max(1, startLine - (int)(visibleCount * loadingBuffer));
		var loadEndLine = Math.Min(this.TotalLines.Value, startLine + (int)(visibleCount * loadingBuffer));

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

		var lines = this._sshService.GetLines(this.OpenedFilePath.Value, loadStartLine, loadEndLine, encoding);

		foreach (var line in lines) {
			this.LoadedLines[line.LineNumber] = line;
		}
	}

	/// <summary>
	/// GREP 実行。クエリが空の場合は結果をクリア。
	/// </summary>
	public void Grep(string query, string encoding) {
		if (this.OpenedFilePath.Value == null) {
			return;
		}
		if ((query?.Length ?? 0) == 0) {
			return;
		}

		this.GrepResults.Clear();
		try {
			this.IsGrepRunning.Value = true;
			this.GrepResults.AddRange(this._sshService.Grep(this.OpenedFilePath.Value, query, 1000, false, encoding));
		} finally {
			this.IsGrepRunning.Value = false;
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
	/// 指定行範囲のテキスト内容 (UTF-8) を取得します。
	/// </summary>
	/// <param name="startLine">開始行 (1 始まり)</param>
	/// <param name="endLine">終了行 (1 始まり)</param>
	/// <param name="encoding">ソースエンコーディング</param>
	/// <returns>結合済みテキスト (末尾改行無し)</returns>
	public string? GetRangeContent(long startLine, long endLine, string encoding) {
		if (this.OpenedFilePath.Value == null) {
			return null;
		}
		if (startLine < 1 || endLine < startLine) {
			return null;
		}
		endLine = Math.Min(endLine, this.TotalLines.Value);
		var lines = this._sshService.GetLines(this.OpenedFilePath.Value, startLine, endLine, encoding);
		return string.Join("\n", lines.Select(l => l.Content));
	}

	/// <summary>
	/// リセット
	/// </summary>
	private void ResetStates() {
		this.TotalLines.Value = 0;
		this.OpenedFilePath.Value = null;
		this.GrepResults.Clear();
		this.LoadedLines.Clear();
		this.Lines.Clear();
	}
}
