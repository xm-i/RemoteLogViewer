using Windows.UI;

namespace RemoteLogViewer.Utils;

internal static class JsonUtils {
	public static string? ColorToHex(Color? color) {
		if (color is not Color c) {
			return null;
		}
		return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
	}

	public static Color? HexToColor(string? hex) {
		if (string.IsNullOrWhiteSpace(hex)) {
			return null;
		}
		hex = hex.Trim();
		if (hex.StartsWith('#')) {
			hex = hex[1..];
		}
		if (hex.Length != 8) {
			return Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
		}

		var a = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
		var r = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
		var g = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);
		var b = byte.Parse(hex[6..8], System.Globalization.NumberStyles.HexNumber);
		return Color.FromArgb(a, r, g, b);
	}

	public static string GuidToString(Guid guid) {
		return guid.ToString();
	}
}
