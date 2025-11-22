using System.Collections.Concurrent;
using System.Threading;

namespace RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;

[Inject(InjectServiceLifetime.Scoped, typeof(IOperationRegistry))]
public class OperationRegistry : IOperationRegistry {
	private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _sources = new();
	public OperationHandle Register(CancellationToken externalToken) {
		var id = Guid.NewGuid();
		var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
		this._sources.TryAdd(id, cts);
		return new OperationHandle(id, cts, this);
	}

	public void Complete(Guid id) {
		if (this._sources.TryRemove(id, out var cts)) {
			try {
				cts.Cancel();
			} catch { }
			cts.Dispose();
		}
	}

	public void CancelAll() {
		foreach (var kv in this._sources.ToArray()) {
			try {
				this._sources.TryRemove(kv);
				kv.Value.Cancel();
				kv.Value.Dispose();
			} catch { }
		}
	}

	public void Dispose() {
		this.CancelAll();
	}
}
