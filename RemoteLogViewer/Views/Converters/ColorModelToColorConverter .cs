using Microsoft.UI;
using Microsoft.UI.Xaml.Data;

using RemoteLogViewer.Composition.Utils.Objects;

using Windows.UI;

namespace RemoteLogViewer.Views.Converters;

public class ColorModelToColorConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, string language) {
		if (value is ColorModel m) {
			return m.ToColor();
		}

		return Colors.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		if (value is Color c) {
			return c.ToColorModel();
		}

		return ColorModel.FromArgb(0, 0xFF, 0xFF, 0xFF);
	}
}
