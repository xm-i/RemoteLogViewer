using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Logging;

using RemoteLogViewer.Core.Models.Ssh.FileViewer.ByteOffsetMap;
using RemoteLogViewer.Core.Services.Ssh;
using RemoteLogViewer.Core.Utils;

namespace RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;

[Inject(InjectServiceLifetime.Scoped, typeof(IBuildByteOffsetMapOperation))]
public sealed class BuildByteOffsetMapOperation : ModelBase<BuildByteOffsetMapOperation>, IBuildByteOffsetMapOperation {
	private readonly IOperationRegistry _operations;
	private readonly ReactiveProperty<ulong> _totalBytes = new(0);

	private readonly ReactiveProperty<bool> _isRunning = new(false);
	public ReadOnlyReactiveProperty<bool> IsRunning {
		get {
			return this._isRunning;
		}
	}

	private readonly ReactiveProperty<ulong> _processedBytes = new(0);
	public ReadOnlyReactiveProperty<ulong> ProcessedBytes {
		get {
			return this._processedBytes;
		}
	}

	public ReadOnlyReactiveProperty<double> Progress {
		get;
	}

	public BuildByteOffsetMapOperation(IOperationRegistry operations, ILogger<BuildByteOffsetMapOperation> logger) : base(logger) {
		this._operations = operations;
		this.Progress = this.ProcessedBytes.CombineLatest(this._totalBytes, (processed, total) => {
			if (total <= 0) {
				return 0;
			}
			return (double)processed / total;
		}).ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);
	}

	public async IAsyncEnumerable<ByteOffset> RunAsync(ISshService sshService, string? filePath, int chunkSize, ulong totalBytes, ByteOffset? startByteOffset, [EnumeratorCancellation] CancellationToken ct) {
		if (string.IsNullOrEmpty(filePath)) {
			yield break;
		}
		using var op = this._operations.Register(ct);
		this._isRunning.Value = true;
		this._processedBytes.Value = 0;
		this._totalBytes.Value = totalBytes;
		try {
			var offsets = sshService.CreateByteOffsetMap(filePath, chunkSize, startByteOffset, op.Token);
			await foreach (var entry in offsets.WithCancellation(op.Token)) {
				this._processedBytes.Value = entry.Bytes - 1;
				yield return entry;
				if (op.Token.IsCancellationRequested) {
					yield break;
				}
			}
		} finally {
			this._isRunning.Value = false;
		}
	}

	public void Reset() {
		this._isRunning.Value = false;
		this._processedBytes.Value = 0;
	}
}
