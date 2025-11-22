namespace RemoteLogViewer.Core.Services.Viewer; 
public class TextRange {
	public int StartIndex;
	public int Length;

	public TextRange(int _StartIndex, int _Length) {
		this.StartIndex = _StartIndex;
		this.Length = _Length;
	}
}
