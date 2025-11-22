using System.Globalization;
using System.Windows.Data;

namespace RemoteLogViewer.WPF.Views.Converters;

/// <summary>
/// bool を反転させます。
/// </summary>
public sealed class BooleanNegationConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is bool b) {
			return !b;
		}
		return value;
	}
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is bool b) {
			return !b;
		}
		throw new NotSupportedException();
	}
}
