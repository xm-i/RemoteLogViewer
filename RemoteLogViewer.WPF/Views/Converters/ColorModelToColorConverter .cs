using System.Globalization;
using System.Windows.Data;

using RemoteLogViewer.Composition.Utils.Objects;

namespace RemoteLogViewer.WPF.Views.Converters;

public class ColorModelToColorConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is ColorModel c) {
			return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
		}

		return "#FFFFFFFF";
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
		if (value is string hex) {
			if (string.IsNullOrWhiteSpace(hex)) {
				return ColorModel.FromArgb(0, 0xFF, 0xFF, 0xFF);
			}
			hex = hex.Trim();
			if (hex.StartsWith('#')) {
				hex = hex[1..];
			}
			if (hex.Length != 8) {
				return ColorModel.FromArgb(0xFF, 0x00, 0x00, 0x00);
			}

			var a = byte.Parse(hex[..2], NumberStyles.HexNumber);
			var r = byte.Parse(hex[2..4], NumberStyles.HexNumber);
			var g = byte.Parse(hex[4..6], NumberStyles.HexNumber);
			var b = byte.Parse(hex[6..8], NumberStyles.HexNumber);
			return ColorModel.FromArgb(a, r, g, b);
		}

		return ColorModel.FromArgb(0, 0xFF, 0xFF, 0xFF);
	}
}
