using System;
using System.Threading;

namespace RemoteLogViewer.Models.Ssh.FileViewer.Operation;

public interface IOperationRegistry {
	public OperationHandle Register(CancellationToken externalToken);
	public void Complete(Guid id);
	public void CancelAll();
}
