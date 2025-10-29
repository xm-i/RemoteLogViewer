using System.Collections.Generic;

namespace RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;

public class ByteOffsetIndex : IByteOffsetIndex {
	private readonly List<ByteOffset> _entries = [];
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

	public void AddRange(IEnumerable<ByteOffset> offsets) {
		this._entries.AddRange(offsets);
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
