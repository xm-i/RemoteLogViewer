using System.Collections.Generic;

namespace RemoteLogViewer.Core.Models.Ssh.FileViewer.ByteOffsetMap;

public interface IByteOffsetIndex {
	public void Reset();
	public void Add(ByteOffset offset);
	public void AddRange(IEnumerable<ByteOffset> offsets);
	public ByteOffset Find(long targetLine);
	public ByteOffset FindLast();
	public int Count {
		get;
	}
}
