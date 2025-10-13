using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RemoteLogViewer.Services.Ssh;

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

	/// <summary>開いているファイル内容。</summary>
	public ReactiveProperty<string?> FileContent {
		get;
	} = new(null);

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
			this.OpenedFilePath.Value = fullPath;
			this.FileContent.Value = content;
		} catch (Exception ex) {
			this.OpenedFilePath.Value = fullPath;
			this.FileContent.Value = $"[Failed to open file: {ex.Message}]";
		}
	}

	/// <summary>
	/// ファイルを閉じます。
	/// </summary>
	public void CloseFile() {
		this.OpenedFilePath.Value = null;
		this.FileContent.Value = null;
	}

}
