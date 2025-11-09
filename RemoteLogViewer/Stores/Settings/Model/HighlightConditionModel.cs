using Windows.UI;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>ハイライト条件。</summary>
[AddScoped]
[GenerateSettingsJsonDto]
public class HighlightConditionModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	public ReactiveProperty<string> Pattern { get; } = new(string.Empty);
	public ReactiveProperty<HighlightPatternType> PatternType { get; } = new(HighlightPatternType.Regex);
	public ReactiveProperty<bool> IgnoreCase { get; } = new(true);
	public ReactiveProperty<bool> HighlightOnlyMatch { get; } = new(false);
	public ReactiveProperty<Color?> ForeColor { get; } = new(null);
	public ReactiveProperty<Color?> BackColor { get; } = new(null);
}

public enum HighlightPatternType {
	Regex,
	Exact
}
