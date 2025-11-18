using System.Collections.Generic;

using RemoteLogViewer.Composition.Utils.Attributes;

namespace RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;

[AddScoped(typeof(IByteOffsetIndex))]
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
		Debug.Assert(this._entries.Count == 0 || this._entries[^1].LineNumber < offset.LineNumber, "Offsets must be added in ascending order of line numbers.");
		this._entries.Add(offset);
	}

	public void AddRange(IEnumerable<ByteOffset> offsets) {
		Debug.Assert(this._entries.Count == 0 || this._entries[^1].LineNumber < offsets.First().LineNumber, "Offsets must be added in ascending order of line numbers.");
		Debug.Assert(offsets.OrderBy(o => o.LineNumber).SequenceEqual(offsets), "Offsets must be in ascending order of line numbers.");
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
