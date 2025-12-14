using System.IO;
using System.Threading;

using RemoteLogViewer.Core.Services.Ssh;

namespace RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;

public interface ISaveRangeContentOperation : IDisposable {
	public ReadOnlyReactiveProperty<bool> IsRunning {
		get;
	}
	public ReadOnlyReactiveProperty<long> TotalLines {
		get;
	}
	public ReadOnlyReactiveProperty<long> SavedLines {
		get;
	}
	public ReadOnlyReactiveProperty<double> Progress {
		get;
	}
	public Task ExecuteAsync(ISshService sshService, string? filePath, StreamWriter writer, long startLine, long endLine, string? encoding, CancellationToken ct);
	public void Reset();
}
