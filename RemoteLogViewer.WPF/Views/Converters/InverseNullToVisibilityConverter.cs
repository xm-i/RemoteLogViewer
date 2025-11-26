using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RemoteLogViewer.WPF.Views.Converters;

/// <summary>
/// 値が null の場合に Visibility.Visible、それ以外は Visibility.Collapsed を返すコンバーターです。
/// </summary>
public sealed class InverseNullToVisibilityConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		return value == null ? Visibility.Visible : Visibility.Collapsed;
	}
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotSupportedException();
	}
}
