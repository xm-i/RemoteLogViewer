using System.Collections.Generic;
using System.Threading;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.Models.Ssh.FileViewer.Operation;

public interface ITailFollowOperation : IDisposable {
	public ReadOnlyReactiveProperty<bool> IsRunning { get; }
	public IAsyncEnumerable<long> RunAsync(ISshService sshService, string? filePath, string? encoding, long currentLastLine, int chunkSize, CancellationToken ct);
}
