using RemoteLogViewer.Models.Ssh.FileViewer.Operation;
using RemoteLogViewer.Models.Ssh.FileViewer.ByteOffsetMap;
using RemoteLogViewer.Services.Ssh;
using Shouldly;
using Moq;
using R3;
using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.Tests.Models.Ssh.FileViewer.Operation;

public class TailFollowOperationTests {
	[Fact]
	public async Task RunAsync_ShouldAddOffsetsOnChunkBoundariesAndFinal() {
		var subject = new Subject<long>();
		var sshMock = new Mock<ISshService>();
		// 行番号列挙
		sshMock.Setup(s => s.TailFollowAsyncOnlyLineNumber("file.log", It.IsAny<ByteOffset>(), 0, It.IsAny<CancellationToken>()))
			.Returns((string _, ByteOffset _, long _, CancellationToken t) => subject.ToAsyncEnumerable(t));
		// バイトオフセット計算 (lineNumber *10 バイトとする)
		sshMock.Setup(s => s.CreateByteOffsetUntilLineAsync("file.log", It.IsAny<ByteOffset>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((string _, ByteOffset start, long target, CancellationToken _) => new ByteOffset(target, (ulong)(target * 10)));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<TailFollowOperation>>();
		var index = new ByteOffsetIndex();
		var op = new TailFollowOperation(opRegistry, index, chunkSize: 1000, loggerMock.Object);

		var list = Observable.ToObservable(op.RunAsync(sshMock.Object, "file.log", null, currentLastLine: 0, CancellationToken.None)).ToLiveList();

		// 初期状態
		list.Count.ShouldBe(0);
		op.IsRunning.CurrentValue.ShouldBeTrue();
		index.Count.ShouldBe(0);

		// 行を流す (1000,1500,2000,2500) -> チャンク境界で1000 と2000, 完了後に2500 の最終オフセット追加
		subject.OnNext(1000);
		await WaitUntilAsync(() => list.Count, 1);
		index.Count.ShouldBe(1); //1000
		index.Find(1500).ShouldBe(new ByteOffset(1000, 1000 * 10UL));

		subject.OnNext(1500);
		await WaitUntilAsync(() => list.Count, 2);
		index.Count.ShouldBe(1); // 境界でないので増えない

		subject.OnNext(2000);
		await WaitUntilAsync(() => list.Count, 3);
		index.Count.ShouldBe(2); //2000追加
		index.Find(2200).ShouldBe(new ByteOffset(2000, 2000 * 10UL));

		subject.OnNext(2500);
		subject.OnCompleted();
		await WaitUntilAsync(() => list.IsCompleted, true);

		// 完了後最終オフセット (2500) が追加される
		index.Count.ShouldBe(3);
		index.Find(3000).ShouldBe(new ByteOffset(2500, 2500 * 10UL));
		op.IsRunning.CurrentValue.ShouldBeFalse();
		list.ShouldBe([1000L, 1500L, 2000L, 2500L]);
	}

	[Fact]
	public async Task RunAsync_Cancel_ShouldNotAddFinalOffset() {
		var subject = new Subject<long>();
		var cts = new CancellationTokenSource();
		var sshMock = new Mock<ISshService>();
		sshMock.Setup(s => s.TailFollowAsyncOnlyLineNumber("file.log", It.IsAny<ByteOffset>(), 0, It.IsAny<CancellationToken>()))
			.Returns((string _, ByteOffset _, long _, CancellationToken t) => subject.ToAsyncEnumerable(t));
		sshMock.Setup(s => s.CreateByteOffsetUntilLineAsync("file.log", It.IsAny<ByteOffset>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((string _, ByteOffset start, long target, CancellationToken _) => new ByteOffset(target, (ulong)(target * 10)));

		using var opRegistry = new OperationRegistry();
		var loggerMock = new Mock<ILogger<TailFollowOperation>>();
		var index = new ByteOffsetIndex();
		var op = new TailFollowOperation(opRegistry, index, chunkSize: 1000, loggerMock.Object);

		var list = Observable.ToObservable(op.RunAsync(sshMock.Object, "file.log", null, currentLastLine: 0, cts.Token)).ToLiveList();

		subject.OnNext(1000); // 境界 ->追加
		await WaitUntilAsync(() => list.Count, 1);
		index.Count.ShouldBe(1);

		subject.OnNext(1500); // 境界外
		await WaitUntilAsync(() => list.Count, 2);
		index.Count.ShouldBe(1);

		// キャンセル
		cts.Cancel();
		//さらに行を流しても受理されないはず
		subject.OnNext(2000);
		subject.OnCompleted();
		await WaitUntilAsync(() => list.IsCompleted, true);

		index.Count.ShouldBe(1); //2000 のオフセットも最終オフセット (1500/2000/...)も追加されない
		index.Find(3000).ShouldBe(new ByteOffset(1000, 1000 * 10UL));
		list.ShouldBe([1000L, 1500L]);
		op.IsRunning.CurrentValue.ShouldBeFalse();
	}
}
