using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Utils.Extensions;

namespace RemoteLogViewer.Models.Ssh.FileViewer;

[AddScoped]
public class TextFileViewerModel {
	private readonly SshService _sshService;
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
	///     ファイルを開き内容を取得します。
	/// </summary>
	/// <param name="path">パス。</param>
	/// <param name="fso">ファイル。</param>
	public void OpenFile(string path, FileSystemObject fso) {
		if (fso.FileSystemObjectType is not (FileSystemObjectType.File or FileSystemObjectType.Symlink)) {
			return;
		}
		var fullPath = path == "/" ? "/" + fso.FileName : path.TrimEnd('/') + "/" + fso.FileName;
		var escaped = fullPath.Replace("\"", "\\\"");
		try {
			this.TotalLines.Value = this._sshService.GetLineCount(fullPath);
			this.AvailableEncodings.Clear();
			this.OpenedFilePath.Value = fullPath;
		} catch {
			this.OpenedFilePath.Value = fullPath;
		}
	}

	public void LoadAvailableEncoding() {
		this.AvailableEncodings.AddRange(this._sshService.ListIconvEncodings());
	}

	public void LoadLines(long startLine, long endLine,string encoding) {
		if (this.OpenedFilePath.Value == null) {
			return;
		}
		this.Lines.Clear();
		this.Lines.AddRange(this._sshService.GetLines(this.OpenedFilePath.Value, startLine, endLine, encoding));
	}

	/// <summary>
	/// GREP 実行。クエリが空の場合は結果をクリア。
	/// </summary>
	public void Grep(string query, string encoding) {
		if (this.OpenedFilePath.Value == null) {
			return;
		}
		if (query.Length == 0) {
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
		this.GrepResults.Clear();
	}

}
