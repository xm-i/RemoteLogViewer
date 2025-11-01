using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Extensions.Logging;
using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;

namespace RemoteLogViewer.Models.Ssh.FileViewer.Operation;

public sealed class GrepOperation : ModelBase<GrepOperation> {
	public GrepOperation(IOperationRegistry operationRegistry, ReadOnlyReactiveProperty<long> totalLineCountProperty, ILogger<GrepOperation> logger) : base(logger) {
		this._operationRegistry = operationRegistry;
		this.TotalLineCount = totalLineCountProperty;
		this.Progress = this.ReceivedLineCount.CombineLatest(this.TotalLineCount, (received, total) => {
			if (total <= 0) {
				return 0;
			}
			return (double)received / total;
		}).ToReadOnlyReactiveProperty().AddTo(this.CompositeDisposable);
	}

	private readonly IOperationRegistry _operationRegistry;

	private readonly ReactiveProperty<bool> _isRunning = new(false);
	public ReadOnlyReactiveProperty<bool> IsRunning {
		get {
			return this._isRunning;
		}
	}

	public ReadOnlyReactiveProperty<long> TotalLineCount {
		get;
	}

	private readonly ReactiveProperty<long> _receivedLineCount = new(0);
	public ReadOnlyReactiveProperty<long> ReceivedLineCount {
		get {
			return this._receivedLineCount;
		}
	}

	public ReadOnlyReactiveProperty<double> Progress {
		get;
	}

	public async IAsyncEnumerable<TextLine> RunAsync(SshService sshService, string? filePath, string? query, string? encoding, ByteOffset startOffset, long startLine, int maxResults, [EnumeratorCancellation] CancellationToken ct) {
		this._receivedLineCount.Value = 0;
		if (string.IsNullOrEmpty(filePath)) {
			yield break;
		}

		if (string.IsNullOrEmpty(query)) {
			yield break;
		}
		using var op = this._operationRegistry.Register(ct);

		this._isRunning.Value = true;
		try {
			var lines = sshService.GrepAsync(filePath, query, false, encoding, maxResults, startOffset, startLine, op.Token);
			await foreach (var line in lines.WithCancellation(op.Token)) {
				this._receivedLineCount.Value = line.LineNumber;
				yield return line;
				if (ct.IsCancellationRequested) {
					break;
				}
			}
		} finally {
			this._isRunning.Value = false;
		}
	}
}
