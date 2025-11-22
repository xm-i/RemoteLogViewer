using System.Globalization;
using System.Windows.Data;

using RemoteLogViewer.Core.Services.Ssh;

namespace RemoteLogViewer.WPF.Views.Converters;

/// <summary>
///     FileSystemObjectType からアイコン用グリフ文字列を返します。
/// </summary>
public sealed class FileSystemObjectTypeToGlyphConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is not FileSystemObjectType fsoType) {
			return string.Empty;
		}
		return fsoType switch {
			FileSystemObjectType.Directory => "\uED25", // フォルダー
			FileSystemObjectType.SymlinkDirectory => "\uE71B", // ディレクトリリンク
			FileSystemObjectType.SymlinkFile => "\uE71B", // ファイルリンク
			FileSystemObjectType.File => "\uE8A5", // ファイル
			_ => "\uE8A5"
		};
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotSupportedException();
	}
}
