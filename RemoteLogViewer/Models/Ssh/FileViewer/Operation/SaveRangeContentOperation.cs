using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffset;

namespace RemoteLogViewer.Models.Ssh.FileViewer.Operation;

public sealed class SaveRangeContentOperation {
	private readonly IOperationRegistry _operations;
	private readonly IByteOffsetIndex _byteOffsetIndex;
	private readonly ReactiveProperty<bool> _isRunning = new(false);
	public ReadOnlyReactiveProperty<bool> IsRunning {
		get {
			return this._isRunning;
		}
	}

	private readonly ReactiveProperty<long> _totalLines = new();
	public ReadOnlyReactiveProperty<long> TotalLines {
		get {
			return this._totalLines;
		}
	}

	private readonly ReactiveProperty<long> _savedLines = new();
	public ReadOnlyReactiveProperty<long> SavedLines {
		get {
			return this._savedLines;
		}
	}

	public ReadOnlyReactiveProperty<double> Progress {
		get;
	}

	public SaveRangeContentOperation(IOperationRegistry operations, IByteOffsetIndex byteOffsetIndex) {
		this._operations = operations;
		this._byteOffsetIndex = byteOffsetIndex;
		this.Progress = this.SavedLines.CombineLatest(this.TotalLines, (saved, total) => {
			if (total <= 0) {
				return 0d;
			}
			return (double)saved / total;
		}).ToReadOnlyReactiveProperty();
	}

	public async Task ExecuteAsync(SshService sshService, string? filePath, StreamWriter writer, long startLine, long endLine, string? encoding, CancellationToken ct) {
		using var op = this._operations.Register(ct);

		if (string.IsNullOrEmpty(filePath)) {
			return;
		}
		if (startLine < 1 || endLine < startLine) {
			return;
		}
		this._totalLines.Value = endLine - startLine + 1;
		this._savedLines.Value = 0;
		this._isRunning.Value = true;
		try {
			var byteOffset = this._byteOffsetIndex.Find(startLine);
			var lines = sshService.GetLinesAsync(filePath, startLine, endLine, encoding, byteOffset, op.Token);
			var total = endLine - startLine + 1;
			long current = 0;
			await foreach (var line in lines.WithCancellation(op.Token)) {
				await writer.WriteLineAsync(line.Content);
				current++;
				if (op.Token.IsCancellationRequested) {
					break;
				}
			}
		} finally {
			this._isRunning.Value = false;
		}
	}

	public void Reset() {
		this._isRunning.Value = false;
		this._totalLines.Value = 0;
		this._savedLines.Value = 0;
	}
}
