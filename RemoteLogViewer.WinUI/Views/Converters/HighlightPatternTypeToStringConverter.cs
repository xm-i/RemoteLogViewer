using Microsoft.UI.Xaml.Data;

using RemoteLogViewer.Composition.Stores.Settings;

namespace RemoteLogViewer.WinUI.Views.Converters;

/// <summary>
/// HighlightPatternType を表示用文字列へ変換するコンバーター。逆変換もサポートします。
/// </summary>
public sealed class HighlightPatternTypeToStringConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is HighlightPatternType hpt) {
			return hpt switch {
				HighlightPatternType.Regex => "Regex",
				HighlightPatternType.Exact => "Exact",
				_ => value.ToString() ?? string.Empty
			};
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotImplementedException();
	}
}
