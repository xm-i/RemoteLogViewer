using System.Collections.Generic;
using System.Threading;
using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.Models.Ssh.FileViewer.Operation;

public interface IBuildByteOffsetMapOperation : IDisposable {
	public ReadOnlyReactiveProperty<bool> IsRunning { get; }
	public ReadOnlyReactiveProperty<ulong> ProcessedBytes { get; }
	public ReadOnlyReactiveProperty<double> Progress { get; }
	public IAsyncEnumerable<ByteOffset> RunAsync(ISshService sshService, string? filePath, int chunkSize, ulong totalBytes, CancellationToken ct);
	public void Reset();
}
