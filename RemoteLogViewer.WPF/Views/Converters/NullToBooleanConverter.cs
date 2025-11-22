using Microsoft.UI.Xaml.Data;

namespace RemoteLogViewer.WinUI.Views.Converters;

/// <summary>
///     null でない場合 true を返すコンバーターです。
/// </summary>
public sealed class NullToBooleanConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) {
        throw new NotSupportedException();
    }
}
