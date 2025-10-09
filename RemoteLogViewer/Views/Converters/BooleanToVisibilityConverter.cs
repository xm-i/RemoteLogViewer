using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace RemoteLogViewer.Views.Converters;

/// <summary>
///     bool を Visibility に変換します。
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        if (value is bool b) {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        if (value is Visibility v) {
            return v == Visibility.Visible;
        }
        return false;
    }
}
