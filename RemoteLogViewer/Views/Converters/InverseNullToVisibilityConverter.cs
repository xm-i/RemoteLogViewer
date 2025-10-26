using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace RemoteLogViewer.Views.Converters;

/// <summary>
/// 値が null の場合に Visibility.Visible、それ以外は Visibility.Collapsed を返すコンバーターです。
/// </summary>
public sealed class InverseNullToVisibilityConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		return value == null ? Visibility.Visible : Visibility.Collapsed;
	}
	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotSupportedException();
	}
}
