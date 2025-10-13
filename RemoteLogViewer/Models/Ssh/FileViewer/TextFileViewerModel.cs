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

	/// <summary>
	///     ファイルを開き内容を取得します。
	/// </summary>
	/// <param name="fso">ファイル。</param>
	public void OpenFile(string path,FileSystemObject fso) {
		if (fso.FileSystemObjectType is not (FileSystemObjectType.File or FileSystemObjectType.Symlink)) {
			return;
		}
		var fullPath = path == "/" ? "/" + fso.FileName : path.TrimEnd('/') + "/" + fso.FileName;
		var escaped = fullPath.Replace("\"", "\\\"");
		try {
			var content = this._sshService.Run($"cat \"{escaped}\"");
			this.TotalLines.Value = this._sshService.GetLineCount(escaped);
			this.OpenedFilePath.Value = fullPath;
		} catch (Exception) {
			this.OpenedFilePath.Value = fullPath;
		}
	}

	public void LoadLines(long startLine, long endLine) {
		if(this.OpenedFilePath.Value == null) {
			return;
		}
		this.Lines.Clear();
		this.Lines.AddRange(this._sshService.GetLines(this.OpenedFilePath.Value, startLine, endLine));
	}

	/// <summary>
	/// ファイルを閉じます。
	/// </summary>
	public void CloseFile() {
		this.OpenedFilePath.Value = null;
	}

}
