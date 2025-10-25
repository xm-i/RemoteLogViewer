using System.Collections.Generic;

namespace RemoteLogViewer.Models.Ssh.FileViewer.ByteOffset;

public class ByteOffsetIndex : IByteOffsetIndex {
	private readonly List<ByteOffset> _entries = new();
	public int Count {
		get {
			return this._entries.Count;
		}
	}
	public void Reset() {
		this._entries.Clear();
	}

	public void Add(ByteOffset offset) {
		this._entries.Add(offset);
	}

	public ByteOffset Find(long targetLine) {
		ByteOffset result = new(0, 0);
		foreach (var bo in this._entries) {
			if (bo.LineNumber < targetLine) {
				result = bo;
			} else {
				break;
			}
		}
		return result;
	}
}
