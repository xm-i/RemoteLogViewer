using System.IO;

using RemoteLogViewer.Models.Ssh;
using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Utils;

namespace RemoteLogViewer.ViewModels.Ssh;

/// <summary>
/// ディレクトリエントリ表示用 ViewModel です。ブックマーク状態を保持します。
/// </summary>
public class FileSystemEntryViewModel {
	private readonly SshSessionModel _sessionModel;
	private bool _suppress;
	/// <summary>元のファイルシステムオブジェクト。</summary>
	public FileSystemObject Original {
		get;
	}
	/// <summary>ファイル名。</summary>
	public string FileName {
		get {
			return this.Original.FileName;
		}
	}
	/// <summary>種別。</summary>
	public FileSystemObjectType? FileSystemObjectType {
		get {
			return this.Original.FileSystemObjectType;
		}
	}

	/// <summary>ブックマーク状態。</summary>
	public BindableReactiveProperty<bool> IsBookmarked { get; } = new();

	public FileSystemEntryViewModel(FileSystemObject fso, SshSessionModel sessionModel) {
		this.Original = fso;
		this._sessionModel = sessionModel;
		var bookmarks = sessionModel.SelectedSshConnectionInfo.Value!.Bookmarks;
		this.IsBookmarked.Value = bookmarks.Any(x => x.Path.Value == PathUtils.CombineUnixPath(this.Original.Path, this.Original.FileName));

		this.IsBookmarked.Subscribe(x => {
			var isExists = bookmarks.Any(x => x.Path.Value == PathUtils.CombineUnixPath(this.Original.Path, this.Original.FileName));
			if (x && !isExists) {
				sessionModel.AddBookmark(this.Original);
			}
		});
	}
}
