using System.Collections.Generic;
using System.Threading;
using RemoteLogViewer.Core.Services.Ssh;

namespace RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;

public interface ITailFollowOperation : IDisposable {
	public ReadOnlyReactiveProperty<bool> IsRunning { get; }
	public IAsyncEnumerable<long> RunAsync(ISshService sshService, string? filePath, string? encoding, long currentLastLine, int chunkSize, CancellationToken ct);
}
