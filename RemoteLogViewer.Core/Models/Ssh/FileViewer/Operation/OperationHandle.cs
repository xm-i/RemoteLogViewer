using System.Threading;

namespace RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;

/// <summary>
///     操作のハンドル。Disposeで操作を完了します。
/// </summary>
public sealed class OperationHandle : IDisposable {
	private readonly IOperationRegistry _registry;
	public Guid Id {
		get;
	}
	public CancellationTokenSource CancellationTokenSource {
		get;
	}
	public CancellationToken Token {
		get {
			return this.CancellationTokenSource.Token;
		}
	}

	public OperationHandle(Guid id, CancellationTokenSource cts, IOperationRegistry registry) {
		this.Id = id;
		this.CancellationTokenSource = cts;
		this._registry = registry;
	}
	public void Dispose() {
		this._registry.Complete(this.Id);
	}
}