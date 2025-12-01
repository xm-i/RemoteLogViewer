using Microsoft.Extensions.Logging;
using Moq;
using R3;
using RemoteLogViewer.Core.Models.Ssh.FileViewer.ByteOffsetMap;
using RemoteLogViewer.Core.Models.Ssh.FileViewer.Operation;
using RemoteLogViewer.Core.Services.Ssh;
using Shouldly;

namespace RemoteLogViewer.Core.Tests.Models.Ssh.FileViewer.Operation;

public class BuildByteOffsetMapOperationTests {
	[Fact]
	public async Task RunAsync_ShouldPublishOffsetsAndProgress() {
		var subject = new Subject<ByteOffset>();

		var sshMock = new Mock<ISshService>();
		sshMock.Setup(s => s.CreateByteOffsetMap("file.log", 100, null, It.IsAny<CancellationToken>())).Returns((string _, int _, CancellationToken t) => subject.ToAsyncEnumerable(t));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<BuildByteOffsetMapOperation>>();
		var op = new BuildByteOffsetMapOperation(opRegistry, loggerMock.Object);

		var list = Observable.ToObservable(op.RunAsync(sshMock.Object, "file.log", 100, 10000, null, CancellationToken.None)).ToLiveList();

		var line100 = new ByteOffset(100, 1000);
		var line200 = new ByteOffset(200, 4000);
		var line300 = new ByteOffset(300, 6000);
		var line400 = new ByteOffset(400, 10000);

		// check
		Assert.Equal([], list);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(0);
		op.ProcessedBytes.CurrentValue.ShouldBe(0UL);

		// publsih
		subject.OnNext(line100);
		await WaitUntilAsync(() => list.Count, 1);

		// check
		Assert.Equal([line100], list);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(0.1, 0.001);
		op.ProcessedBytes.CurrentValue.ShouldBe(1000UL);

		// publsih
		subject.OnNext(line200);
		await WaitUntilAsync(() => list.Count, 2);

		// check
		Assert.Equal([line100, line200], list);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(0.4, 0.001);
		op.ProcessedBytes.CurrentValue.ShouldBe(4000UL);

		// publsih
		subject.OnNext(line300);
		await WaitUntilAsync(() => list.Count, 3);

		// check
		Assert.Equal([line100, line200, line300], list);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(0.6, 0.001);
		op.ProcessedBytes.CurrentValue.ShouldBe(6000UL);

		// publsih
		subject.OnNext(line400);
		await WaitUntilAsync(() => list.Count, 4);

		// check
		Assert.Equal([line100, line200, line300, line400], list);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(1, 0.001);
		op.ProcessedBytes.CurrentValue.ShouldBe(10000UL);

		subject.OnCompleted();
		await WaitUntilAsync(() => list.IsCompleted, true);

		// check
		Assert.Equal([line100, line200, line300, line400], list);
		op.IsRunning.CurrentValue.ShouldBeFalse();
		op.Progress.CurrentValue.ShouldBe(1, 0.001);
		op.ProcessedBytes.CurrentValue.ShouldBe(10000UL);
	}

	[Fact]
	public async Task RunAsync_Cancel_ShouldStopEarly() {
		var cts = new CancellationTokenSource();
		var subject = new Subject<ByteOffset>();

		var sshMock = new Mock<ISshService>();
		sshMock.Setup(s => s.CreateByteOffsetMap("file.log", 100, null, It.IsAny<CancellationToken>())).Returns((string _, int _, CancellationToken t) => subject.ToAsyncEnumerable(t));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<BuildByteOffsetMapOperation>>();
		var op = new BuildByteOffsetMapOperation(opRegistry, loggerMock.Object);

		var list = Observable.ToObservable(op.RunAsync(sshMock.Object, "file.log", 100, 10000, null, cts.Token)).ToLiveList();

		var line100 = new ByteOffset(100, 1000);
		var line200 = new ByteOffset(200, 4000);

		// publsih
		subject.OnNext(line100);
		// publsih
		subject.OnNext(line200);
		await WaitUntilAsync(() => list.Count, 2);

		// check
		Assert.Equal([line100, line200], list);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		op.Progress.CurrentValue.ShouldBe(0.4, 0.01);
		op.ProcessedBytes.CurrentValue.ShouldBe(4000UL);

		cts.Cancel();
		await Task.Delay(10);
		await WaitUntilAsync(() => list.IsCompleted, true);

		// check
		Assert.Equal([line100, line200], list);
		op.IsRunning.CurrentValue.ShouldBeFalse();
		op.Progress.CurrentValue.ShouldBe(0.4, 0.01);
		op.ProcessedBytes.CurrentValue.ShouldBe(4000UL);
	}
}
