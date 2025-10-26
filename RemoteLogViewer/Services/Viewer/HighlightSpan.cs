using System.Collections.Generic;

using Microsoft.UI.Xaml.Documents;

namespace RemoteLogViewer.Services.Viewer;

public class HighlightSpan() {
	public TextStyle Style {
		get;
	} = new();

	public required IList<TextRange> Ranges {
		get;
		init;
	}
}