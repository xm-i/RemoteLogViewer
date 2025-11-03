using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Logging;

using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;

namespace RemoteLogViewer.Models.Ssh.FileViewer.Operation;

public sealed class TailFollowOperation : ModelBase<TailFollowOperation> {
	private readonly IOperationRegistry _operations;
	private readonly IByteOffsetIndex _byteOffsetIndex;
	private readonly int _chunkSize;
	private readonly ReactiveProperty<bool> _isRunning = new(false);
	public ReadOnlyReactiveProperty<bool> IsRunning {
		get {
			return this._isRunning;
		}
	}
	public TailFollowOperation(IOperationRegistry operations, IByteOffsetIndex byteOffsetIndex, int chunkSize, ILogger<TailFollowOperation> logger) : base(logger) {
		this._operations = operations;
		this._byteOffsetIndex = byteOffsetIndex;
		this._chunkSize = chunkSize;
	}

	public async IAsyncEnumerable<long> RunAsync(ISshService sshService, string? filePath, string? encoding, long currentLastLine, [EnumeratorCancellation] CancellationToken ct) {
		if (string.IsNullOrEmpty(filePath)) {
			yield break;
		}
		using var op = this._operations.Register(ct);
		this._isRunning.Value = true;
		try {
			var startOffset = this._byteOffsetIndex.Find(currentLastLine);
			var lines = sshService.TailFollowAsyncOnlyLineNumber(filePath, startOffset, currentLastLine, op.Token);
			var lastLineNumber = currentLastLine;
			await foreach (var lineNumber in lines.WithCancellation(op.Token)) {
				if (lineNumber % this._chunkSize == 0) {
					var prevOffset = this._byteOffsetIndex.Find(lineNumber);
					var newOffset = await sshService.CreateByteOffsetUntilLineAsync(filePath!, prevOffset, lineNumber, op.Token);
					this._byteOffsetIndex.Add(newOffset);
				}
				lastLineNumber = lineNumber;
				yield return lineNumber;
				if (ct.IsCancellationRequested) {
					break;
				}
			}
			if (!ct.IsCancellationRequested) {
				var prevOffset2 = this._byteOffsetIndex.Find(lastLineNumber);
				var finalOffset = await sshService.CreateByteOffsetUntilLineAsync(filePath!, prevOffset2, lastLineNumber, CancellationToken.None);
				this._byteOffsetIndex.Add(finalOffset);
			}
		} finally {
			this._isRunning.Value = false;
		}
	}
}
