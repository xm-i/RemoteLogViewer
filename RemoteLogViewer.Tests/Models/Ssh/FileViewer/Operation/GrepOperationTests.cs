using RemoteLogViewer.Models.Ssh.FileViewer.Operation;
using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;
using RemoteLogViewer.Services.Ssh;
using RemoteLogViewer.Models.Ssh.FileViewer; // TextLine
using Shouldly;
using Moq;
using R3;
using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.Tests.Models.Ssh.FileViewer.Operation;

public class GrepOperationTests {
	[Fact]
	public async Task RunAsync_ShouldPublishLinesAndProgress() {
		var subject = new Subject<TextLine>();
		var sshMock = new Mock<ISshService>();
		sshMock.Setup(s => s.GrepAsync("file.log", "ERR", false, null,100, It.IsAny<ByteOffset>(),1, It.IsAny<CancellationToken>()))
			.Returns((string _, string _, bool _, string? _, int _, ByteOffset bo, long _, CancellationToken t) => subject.ToAsyncEnumerable(t));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<GrepOperation>>();
		var totalLines = new ReactiveProperty<long>(1000);
		var op = new GrepOperation(opRegistry, totalLines.ToReadOnlyReactiveProperty(), loggerMock.Object);

		var list = Observable.ToObservable(op.RunAsync(sshMock.Object, "file.log", "ERR", null, new ByteOffset(0,0),1,100, CancellationToken.None)).ToLiveList();

		var l1 = new TextLine(10, "ERR first");
		var l2 = new TextLine(200, "ERR second");

		list.Count.ShouldBe(0);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(0);
		op.ReceivedLineCount.CurrentValue.ShouldBe(0);

		subject.OnNext(l1);
		await WaitUntilAsync(() => list.Count,1);
		list.ShouldBe([l1]);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(l1.LineNumber / (double)totalLines.Value,0.001);
		op.ReceivedLineCount.CurrentValue.ShouldBe(10);

		subject.OnNext(l2);
		await WaitUntilAsync(() => list.Count,2);
		list.ShouldBe([l1, l2]);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(l2.LineNumber / (double)totalLines.Value,0.001);
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
		sshMock.Setup(s => s.GrepAsync("file.log", "ERR", false, null,100, It.IsAny<ByteOffset>(),1, It.IsAny<CancellationToken>()))
			.Returns((string _, string _, bool _, string? _, int _, ByteOffset bo, long _, CancellationToken t) => subject.ToAsyncEnumerable(t));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<GrepOperation>>();
		var totalLines = new ReactiveProperty<long>(1000);
		var op = new GrepOperation(opRegistry, totalLines.ToReadOnlyReactiveProperty(), loggerMock.Object);

		var list = Observable.ToObservable(op.RunAsync(sshMock.Object, "file.log", "ERR", null, new ByteOffset(0,0),1,100, cts.Token)).ToLiveList();

		var l1 = new TextLine(10, "ERR first");
		var l2 = new TextLine(200, "ERR second");

		subject.OnNext(l1);
		subject.OnNext(l2);
		await WaitUntilAsync(() => list.Count,2);

		list.ShouldBe([l1, l2]);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(l2.LineNumber / (double)totalLines.Value,0.001);
		op.ReceivedLineCount.CurrentValue.ShouldBe(200);

		cts.Cancel();
		await Task.Delay(10);
		await WaitUntilAsync(() => list.IsCompleted, true);

		list.ShouldBe([l1, l2]);
		op.IsRunning.CurrentValue.ShouldBeFalse();
	}
}
