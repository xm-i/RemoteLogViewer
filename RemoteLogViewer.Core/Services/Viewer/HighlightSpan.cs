using System.Collections.Generic;

namespace RemoteLogViewer.Core.Services.Viewer;

public class HighlightSpan() {
	public TextStyle Style {
		get;
	} = new();

	public required IList<TextRange> Ranges {
		get;
		init;
	}
}