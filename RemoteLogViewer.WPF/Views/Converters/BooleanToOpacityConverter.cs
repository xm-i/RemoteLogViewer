using System.Globalization;
using System.Windows.Data;

namespace RemoteLogViewer.WPF.Views.Converters;

/// <summary>
/// bool を 1 / 0 (Opacity) に変換します。
/// </summary>
public class BooleanToOpacityConverter : IValueConverter {
	/// <summary>
	/// 変換します。
	/// </summary>
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is bool b && b) {
			return 1.0;
		}
		return 0.2;
	}

	/// <summary>
	/// 逆変換は未使用です。
	/// </summary>
	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		throw new NotSupportedException();
	}
}
