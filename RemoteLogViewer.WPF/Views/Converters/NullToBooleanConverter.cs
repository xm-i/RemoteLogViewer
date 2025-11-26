using System.Globalization;
using System.Windows.Data;

namespace RemoteLogViewer.WPF.Views.Converters;

/// <summary>
///     null でない場合 true を返すコンバーターです。
/// </summary>
public sealed class NullToBooleanConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}
