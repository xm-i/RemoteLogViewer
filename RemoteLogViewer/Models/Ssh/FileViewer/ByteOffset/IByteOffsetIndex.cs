namespace RemoteLogViewer.Models.Ssh.FileViewer.ByteOffset;

public interface IByteOffsetIndex {
	public void Reset();
	public void Add(ByteOffset offset);
	public ByteOffset Find(long targetLine);
	public int Count {
		get;
	}
}
