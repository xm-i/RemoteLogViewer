using Microsoft.UI.Xaml.Data;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.Views.Converters;

/// <summary>
///     FileSystemObject からアイコン用グリフ文字列を返します。
/// </summary>
public sealed class FileSystemObjectToGlyphConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is not FileSystemObject fso) {
			return "";
		}
		return fso.FileSystemObjectType switch {
			FileSystemObjectType.Directory => "\uED25", // フォルダー
			FileSystemObjectType.Symlink => "\uE71B",   // リンク
			FileSystemObjectType.File => "\uE8A5",      // ファイル
			_ => "\uE8A5"
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotSupportedException();
	}
}
