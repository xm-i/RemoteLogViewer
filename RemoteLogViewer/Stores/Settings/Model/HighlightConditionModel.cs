using Microsoft.Extensions.DependencyInjection;

using Windows.UI;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>ハイライト条件。</summary>
[AddScoped]
public class HighlightConditionModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	public ReactiveProperty<string> Pattern { get; } = new(string.Empty);
	public ReactiveProperty<HighlightPatternType> PatternType { get; } = new(HighlightPatternType.Regex);
	public ReactiveProperty<bool> IgnoreCase { get; } = new(true);
	public ReactiveProperty<bool> HighlightOnlyMatch { get; } = new(false);
	public ReactiveProperty<Color> ForeColor { get; } = new(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
	public ReactiveProperty<Color> BackColor { get; } = new(Color.FromArgb(0xFF, 0xFF, 0xFF, 0x00));
}

public enum HighlightPatternType {
	Regex,
	Exact
}

public class HighlightConditionModelForJson {
	public string Pattern { get; set; } = string.Empty;
	public HighlightPatternType PatternType { get; set; } = HighlightPatternType.Regex;
	public bool IgnoreCase { get; set; } = true;
	public bool HighlightOnlyMatch { get; set; } = false;
	public string ForeColor { get; set; } = "#FF000000";
	public string BackColor { get; set; } = "#FFFFFF00";

	private static string ColorToHex(Color c) {
		return $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
	}

	private static Color HexToColor(string hex) {
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

	public static HighlightConditionModel CreateModel(HighlightConditionModelForJson json, IServiceProvider service) {
		var scope = service.CreateScope();
		var model = scope.ServiceProvider.GetRequiredService<HighlightConditionModel>();
		model.Pattern.Value = json.Pattern;
		model.PatternType.Value = json.PatternType;
		model.IgnoreCase.Value = json.IgnoreCase;
		model.HighlightOnlyMatch.Value = json.HighlightOnlyMatch;
		model.ForeColor.Value = HexToColor(json.ForeColor);
		model.BackColor.Value = HexToColor(json.BackColor);
		return model;
	}
	public static HighlightConditionModelForJson CreateJson(HighlightConditionModel model) {
		return new HighlightConditionModelForJson {
			Pattern = model.Pattern.Value,
			PatternType = model.PatternType.Value,
			IgnoreCase = model.IgnoreCase.Value,
			HighlightOnlyMatch = model.HighlightOnlyMatch.Value,
			ForeColor = ColorToHex(model.ForeColor.Value),
			BackColor = ColorToHex(model.BackColor.Value)
		};
	}
}
