using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RemoteLogViewer.Models.Ssh;
using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.ViewModels.Ssh.FileViewer;

[AddScoped]
public class TextFileViewerViewModel {
	private readonly TextFileViewerModel _textFileViewerModel;
	public TextFileViewerViewModel(TextFileViewerModel textFileViewerModel) {
		this._textFileViewerModel = textFileViewerModel;
		this.OpenedFilePath = this._textFileViewerModel.OpenedFilePath.ToReadOnlyBindableReactiveProperty();
		this.FileContent = this._textFileViewerModel.FileContent.ToReadOnlyBindableReactiveProperty();
	}

	/// <summary>開いているファイルのパス。</summary>
	public IReadOnlyBindableReactiveProperty<string?> OpenedFilePath {
		get;
	}
	/// <summary>開いているファイル内容。</summary>
	public IReadOnlyBindableReactiveProperty<string?> FileContent {
		get;
	}

	public void OpenFile(string path, FileSystemObject fso) {
		this._textFileViewerModel.OpenFile(path, fso);
	}

}
