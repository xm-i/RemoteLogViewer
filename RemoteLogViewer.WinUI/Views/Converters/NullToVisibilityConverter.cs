using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace RemoteLogViewer.WinUI.Views.Converters;

/// <summary>
/// 値が null の場合に Visibility.Collapsed、それ以外は Visibility.Visible を返すコンバーターです。
/// </summary>
public sealed class NullToVisibilityConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		return value == null ? Visibility.Collapsed : Visibility.Visible;
	}
	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		throw new NotSupportedException();
	}
}
