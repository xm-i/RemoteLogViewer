using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RemoteLogViewer.WPF.Views.Converters;

/// <summary>
/// 値が null の場合に Visibility.Collapsed、それ以外は Visibility.Visible を返すコンバーターです。
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		return value == null ? Visibility.Collapsed : Visibility.Visible;
	}
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotSupportedException();
	}
}
