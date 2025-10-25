using System.Collections.Generic;
using Windows.UI;

namespace RemoteLogViewer.Services.Viewer;

public class TextStyle {
	public Color? ForeColor { get; set; }
	public Color? BackColor { get; set; }
}

public class StyledText {
	public string Text { get; set; } = string.Empty;
	public TextStyle Style { get; set; } = new();
}

public class HighlightedTextLine {
	public long LineNumber { get; set; }
	public IReadOnlyList<StyledText> StyledTexts { get; set; } = new List<StyledText>();
	public TextStyle LineStyle { get; set; } = new();
}
