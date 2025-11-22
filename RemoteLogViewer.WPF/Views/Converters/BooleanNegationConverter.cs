using Microsoft.UI.Xaml.Data;

namespace RemoteLogViewer.WinUI.Views.Converters;

/// <summary>
/// bool を反転させます。
/// </summary>
public sealed class BooleanNegationConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is bool b) {
			return !b;
		}
		return value;
	}
	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		if (value is bool b) {
			return !b;
		}
		throw new NotSupportedException();
	}
}
