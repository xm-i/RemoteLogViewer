using System.Collections.Generic;
using System.Threading;
using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;
using RemoteLogViewer.Services.Ssh;

namespace RemoteLogViewer.Models.Ssh.FileViewer.Operation;

public interface IGrepOperation : IDisposable {
	public ReadOnlyReactiveProperty<bool> IsRunning { get; }
	public ReactiveProperty<long> TotalLineCount { get; }
	public ReadOnlyReactiveProperty<long> ReceivedLineCount { get; }
	public ReadOnlyReactiveProperty<double> Progress { get; }
	public IAsyncEnumerable<TextLine> RunAsync(ISshService sshService, string? filePath, string? query, string? encoding, ByteOffset startOffset, long startLine, int maxResults, CancellationToken ct);
}
