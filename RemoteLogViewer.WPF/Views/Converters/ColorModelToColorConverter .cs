using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using RemoteLogViewer.Composition.Utils.Objects;
using RemoteLogViewer.WPF.Utils;

namespace RemoteLogViewer.WPF.Views.Converters;

public class ColorModelToColorConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is ColorModel m) {
			return m.ToColor();
		}

		return Colors.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is Color c) {
			return c.ToColorModel();
		}

		return ColorModel.FromArgb(0, 0xFF, 0xFF, 0xFF);
	}
}
