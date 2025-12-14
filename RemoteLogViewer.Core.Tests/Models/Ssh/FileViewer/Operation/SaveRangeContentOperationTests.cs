// TextLine

using Microsoft.Extensions.Logging;

using Moq;

using R3;

using RemoteLogViewer.Core.Models.Ssh.FileViewer;
using RemoteLogViewer.Core.Models.Ssh.FileViewer.ByteOffsetMap;
using RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;
using RemoteLogViewer.Core.Services.Ssh;

using Shouldly;

namespace RemoteLogViewer.Core.Tests.Models.Ssh.FileViewer.Operation;

public class SaveRangeContentOperationTests {
	private IByteOffsetIndex CreateIndex(params ByteOffset[] values) {
		var idx = new ByteOffsetIndex();
		idx.AddRange(values);
		return idx;
	}

	[Fact]
	public async Task ExecuteAsync_ShouldWriteAllLinesAndUpdateProgress() {
		var subject = new Subject<TextLine>();
		var sshMock = new Mock<ISshService>();
		sshMock.Setup(s => s.GetLinesAsync("file.log", 1, 3, null, It.IsAny<ByteOffset>(), It.IsAny<CancellationToken>()))
			.Returns((string _, long _, long _, string? _, ByteOffset bo, CancellationToken t) => subject.ToAsyncEnumerable(t));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<SaveRangeContentOperation>>();
		var index = this.CreateIndex(new ByteOffset(0, 0));
		var op = new SaveRangeContentOperation(opRegistry, index, loggerMock.Object);

		var ms = new MemoryStream();
		using var writer = new StreamWriter(ms, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
		var task = op.ExecuteAsync(sshMock.Object, "file.log", writer, 1, 3, null, CancellationToken.None);

		subject.OnNext(new TextLine(1, "A"));
		subject.OnNext(new TextLine(2, "B"));
		subject.OnNext(new TextLine(3, "C"));
		subject.OnCompleted();
		await task;
		await writer.FlushAsync();
		var text = System.Text.Encoding.UTF8.GetString(ms.ToArray()).TrimEnd();

		op.IsRunning.CurrentValue.ShouldBeFalse();
		op.TotalLines.CurrentValue.ShouldBe(3);
		op.SavedLines.CurrentValue.ShouldBe(3);
		op.Progress.CurrentValue.ShouldBe(1.0, 0.001);
		text.ShouldBe("A\nB\nC".Replace("\n", Environment.NewLine));
	}

	[Fact]
	public async Task ExecuteAsync_Cancel_ShouldStopEarly() {
		var cts = new CancellationTokenSource();
		var subject = new Subject<TextLine>();
		var sshMock = new Mock<ISshService>();
		sshMock.Setup(s => s.GetLinesAsync("file.log", 1, 5, null, It.IsAny<ByteOffset>(), It.IsAny<CancellationToken>()))
			.Returns((string _, long _, long _, string? _, ByteOffset bo, CancellationToken t) => subject.ToAsyncEnumerable(t));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<SaveRangeContentOperation>>();
		var index = this.CreateIndex(new ByteOffset(0, 0));
		var op = new SaveRangeContentOperation(opRegistry, index, loggerMock.Object);
		var ms = new MemoryStream();
		using var writer = new StreamWriter(ms, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
		var task = op.ExecuteAsync(sshMock.Object, "file.log", writer, 1, 5, null, cts.Token);

		try {
			subject.OnNext(new TextLine(1, "A"));
			subject.OnNext(new TextLine(2, "B"));
			subject.OnNext(new TextLine(3, "C"));
			await WaitUntilAsync(() => op.SavedLines.CurrentValue, 3);
			cts.Cancel();
			await task;
		} catch (OperationCanceledException) { }
		await writer.FlushAsync();
		var text = System.Text.Encoding.UTF8.GetString(ms.ToArray()).TrimEnd();

		op.IsRunning.CurrentValue.ShouldBeFalse();
		op.TotalLines.CurrentValue.ShouldBe(5);
		op.SavedLines.CurrentValue.ShouldBe(3);
		op.Progress.CurrentValue.ShouldBe(0.6, 0.05);
		text.ShouldBe("A\nB\nC".Replace("\n", Environment.NewLine));
	}

	[Fact]
	public async Task ExecuteAsync_InvalidRange_ShouldDoNothing() {
		var sshMock = new Mock<ISshService>();
		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<SaveRangeContentOperation>>();
		var index = this.CreateIndex(new ByteOffset(0, 0));
		var op = new SaveRangeContentOperation(opRegistry, index, loggerMock.Object);
		var ms = new MemoryStream();
		using var writer = new StreamWriter(ms, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
		await op.ExecuteAsync(sshMock.Object, "file.log", writer, 5, 3, null, CancellationToken.None);
		await writer.FlushAsync();
		op.SavedLines.CurrentValue.ShouldBe(0);
		op.TotalLines.CurrentValue.ShouldBe(0);
		op.IsRunning.CurrentValue.ShouldBeFalse();
		System.Text.Encoding.UTF8.GetString(ms.ToArray()).ShouldBe(string.Empty);
	}

	[Fact]
	public async Task ExecuteAsync_NullPath_ShouldDoNothing() {
		var sshMock = new Mock<ISshService>();
		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<SaveRangeContentOperation>>();
		var index = this.CreateIndex(new ByteOffset(0, 0));
		var op = new SaveRangeContentOperation(opRegistry, index, loggerMock.Object);
		var ms = new MemoryStream();
		using var writer = new StreamWriter(ms, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
		await op.ExecuteAsync(sshMock.Object, null, writer, 1, 3, null, CancellationToken.None);
		await writer.FlushAsync();
		op.IsRunning.CurrentValue.ShouldBeFalse();
		op.SavedLines.CurrentValue.ShouldBe(0);
		op.TotalLines.CurrentValue.ShouldBe(0);
		System.Text.Encoding.UTF8.GetString(ms.ToArray()).ShouldBe(string.Empty);
	}
}
