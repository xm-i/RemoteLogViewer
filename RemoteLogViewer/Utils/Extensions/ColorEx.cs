using RemoteLogViewer.Composition.Utils.Objects;

using Windows.UI;

namespace RemoteLogViewer.Utils.Extensions;

public static class ColorEx {
	public static ColorModel ToColorModel(this Color color) {
		return ColorModel.FromArgb(color.A, color.R, color.G, color.B);
	}

	public static Color ToColor(this ColorModel cm) {
		return Color.FromArgb(cm.A, cm.R, cm.G, cm.B);
	}
}
