using System.Text.Json;
using System.Text.Json.Serialization;
using RemoteLogViewer.Composition.Utils.Objects;

namespace RemoteLogViewer.Core.Stores.Converters;

public class ColorJsonConverter : JsonConverter<ColorModel?> {
	public override ColorModel? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
		var hex = reader.GetString();

		if (string.IsNullOrWhiteSpace(hex)) {
			return null;
		}
		hex = hex.Trim();
		if (hex.StartsWith('#')) {
			hex = hex[1..];
		}
		if (hex.Length != 8) {
			return ColorModel.FromArgb(0xFF, 0x00, 0x00, 0x00);
		}

		var a = byte.Parse(hex[..2], System.Globalization.NumberStyles.HexNumber);
		var r = byte.Parse(hex[2..4], System.Globalization.NumberStyles.HexNumber);
		var g = byte.Parse(hex[4..6], System.Globalization.NumberStyles.HexNumber);
		var b = byte.Parse(hex[6..8], System.Globalization.NumberStyles.HexNumber);
		return ColorModel.FromArgb(a, r, g, b);
	}

	public override void Write(Utf8JsonWriter writer, ColorModel? value, JsonSerializerOptions options) {
		if (value is not ColorModel c) {
			return;
		}
		writer.WriteStringValue($"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}");
	}
}
