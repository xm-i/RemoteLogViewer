using System.Threading;

namespace RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;

public interface IOperationRegistry: IDisposable {
	public OperationHandle Register(CancellationToken externalToken);
	public void Complete(Guid id);
	public void CancelAll();
}
