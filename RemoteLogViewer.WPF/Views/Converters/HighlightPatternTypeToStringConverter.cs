using System.Globalization;
using System.Windows.Data;

using RemoteLogViewer.Composition.Stores.Settings;

namespace RemoteLogViewer.WPF.Views.Converters;

/// <summary>
/// HighlightPatternType を表示用文字列へ変換するコンバーター。逆変換もサポートします。
/// </summary>
public sealed class HighlightPatternTypeToStringConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is HighlightPatternType hpt) {
			return hpt switch {
				HighlightPatternType.Regex => "Regex",
				HighlightPatternType.Exact => "Exact",
				_ => value.ToString() ?? string.Empty
			};
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotImplementedException();
	}
}
