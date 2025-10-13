using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RemoteLogViewer.Models.Ssh.FileViewer;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.ViewModels.Ssh.FileViewer;

/// <summary>
///     テキストファイル閲覧 ViewModel。スクロール位置に応じて部分読み込みを行います。
/// </summary>
[AddScoped]
public class TextFileViewerViewModel {
	private readonly TextFileViewerModel _textFileViewerModel;
	public TextFileViewerViewModel(TextFileViewerModel textFileViewerModel) {
		this._textFileViewerModel = textFileViewerModel;
		this.OpenedFilePath = this._textFileViewerModel.OpenedFilePath.ToReadOnlyBindableReactiveProperty();
		this.TotalLines = this._textFileViewerModel.TotalLines.ToReadOnlyBindableReactiveProperty();
		this.Lines = this._textFileViewerModel.Lines.ToNotifyCollectionChanged();
		this.WindowEndLine = this.WindowStartLine.CombineLatest(this.VisibleLineCount, (start, count) => start + count - 1).ToReadOnlyBindableReactiveProperty((long)0);
		this.LoadLinesCommand.Subscribe(_ => {
			this._textFileViewerModel.LoadLines(this.WindowStartLine.Value + 1, this.WindowEndLine.Value + 1);
		});
	}

	/// <summary>開いているファイルのパス。</summary>
	public IReadOnlyBindableReactiveProperty<string?> OpenedFilePath {
		get;
	}
	/// <summary>総行数。</summary>
	public IReadOnlyBindableReactiveProperty<long> TotalLines {
		get;
	}
	/// <summary>現在ウィンドウ開始行。</summary>
	public BindableReactiveProperty<long> WindowStartLine {
		get;
	} = new();
	/// <summary>現在ウィンドウ終了行。</summary>
	public IReadOnlyBindableReactiveProperty<long> WindowEndLine {
		get;
	}

	/// <summary>表示行一覧。</summary>
	public NotifyCollectionChangedSynchronizedViewList<TextLine> Lines {
		get;
	}

	/// <summary>
	/// 画面上表示可能な行数
	/// </summary>
	public BindableReactiveProperty<long> VisibleLineCount {
		get;
	} = new();

	public ReactiveCommand LoadLinesCommand {
		get;
	} = new();

	/// <summary>
	///     ファイルを開きます。
	/// </summary>
	public void OpenFile(string path, FileSystemObject fso) {
		this._textFileViewerModel.OpenFile(path, fso);
	}
}
