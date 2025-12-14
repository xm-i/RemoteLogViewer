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

public class GrepOperationTests {
	[Fact]
	public async Task RunAsync_ShouldPublishLinesAndProgress() {
		var subject = new Subject<TextLine>();
		var sshMock = new Mock<ISshService>();
		sshMock.Setup(s => s.GrepAsync("file.log", "ERR", null, 100, It.IsAny<ByteOffset>(), 1, false, false, It.IsAny<CancellationToken>()))
			.Returns((string _, string _, bool _, string? _, int _, ByteOffset bo, long _, CancellationToken t) => subject.ToAsyncEnumerable(t));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<GrepOperation>>();
		var op = new GrepOperation(opRegistry, loggerMock.Object);
		op.TotalLineCount.Value = 1000;

		var list = Observable.ToObservable(op.RunAsync(sshMock.Object, "file.log", "ERR", null, new ByteOffset(0, 0), 1, 100, false, false, CancellationToken.None)).ToLiveList();

		var l1 = new TextLine(10, "ERR first");
		var l2 = new TextLine(200, "ERR second");

		list.Count.ShouldBe(0);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(0);
		op.ReceivedLineCount.CurrentValue.ShouldBe(0);

		subject.OnNext(l1);
		await WaitUntilAsync(() => list.Count, 1);
		list.ShouldBe([l1]);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(l1.LineNumber / 1000d, 0.001);
		op.ReceivedLineCount.CurrentValue.ShouldBe(10);

		subject.OnNext(l2);
		await WaitUntilAsync(() => list.Count, 2);
		list.ShouldBe([l1, l2]);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(l2.LineNumber / 1000d, 0.001);
		op.ReceivedLineCount.CurrentValue.ShouldBe(200);

		subject.OnCompleted();
		await WaitUntilAsync(() => list.IsCompleted, true);
		op.IsRunning.CurrentValue.ShouldBeFalse();
		list.ShouldBe([l1, l2]);
	}

	[Fact]
	public async Task RunAsync_Cancel_ShouldStopEarly() {
		var cts = new CancellationTokenSource();
		var subject = new Subject<TextLine>();
		var sshMock = new Mock<ISshService>();
		sshMock.Setup(s => s.GrepAsync("file.log", "ERR", null, 100, It.IsAny<ByteOffset>(), 1, false, false, It.IsAny<CancellationToken>()))
			.Returns((string _, string _, bool _, string? _, int _, ByteOffset bo, long _, CancellationToken t) => subject.ToAsyncEnumerable(t));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<GrepOperation>>();
		var op = new GrepOperation(opRegistry, loggerMock.Object);
		op.TotalLineCount.Value = 1000;

		var list = Observable.ToObservable(op.RunAsync(sshMock.Object, "file.log", "ERR", null, new ByteOffset(0, 0), 1, 100, false, false, cts.Token)).ToLiveList();

		var l1 = new TextLine(10, "ERR first");
		var l2 = new TextLine(200, "ERR second");

		subject.OnNext(l1);
		subject.OnNext(l2);
		await WaitUntilAsync(() => list.Count, 2);

		list.ShouldBe([l1, l2]);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(l2.LineNumber / 1000d, 0.001);
		op.ReceivedLineCount.CurrentValue.ShouldBe(200);

		cts.Cancel();
		await Task.Delay(10);
		await WaitUntilAsync(() => list.IsCompleted, true);

		list.ShouldBe([l1, l2]);
		op.IsRunning.CurrentValue.ShouldBeFalse();
	}
}
