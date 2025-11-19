using System;

using R3;
using R3.JsonConfig.Attributes;

using RemoteLogViewer.Composition.Utils.Objects;

namespace RemoteLogViewer.Composition.Stores.Settings;

/// <summary>ハイライト条件。</summary>
[Inject(InjectServiceLifetime.Scoped)]
[GenerateR3JsonConfigDto]
public class HighlightConditionModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	public ReactiveProperty<string> Pattern { get; } = new(string.Empty);
	public ReactiveProperty<HighlightPatternType> PatternType { get; } = new(HighlightPatternType.Regex);
	public ReactiveProperty<bool> IgnoreCase { get; } = new(true);
	public ReactiveProperty<bool> HighlightOnlyMatch { get; } = new(false);
	public ReactiveProperty<ColorModel?> ForeColor { get; } = new(null);
	public ReactiveProperty<ColorModel?> BackColor { get; } = new(null);
}

public enum HighlightPatternType {
	Regex,
	Exact
}
