using Microsoft.Extensions.Time.Testing;

using R3;

using RemoteLogViewer.Utils.Extensions;

using Shouldly;

namespace RemoteLogViewer.Tests.Utils.Extensions;

public class ObservableExTests {
	#region ChunkForAddRange
	[Fact]
	public async Task ChunkForAddRange_ShouldCreateChunksBasedOnTimeInterval() {
		// Arrange
		var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
		var maxInterval = TimeSpan.FromSeconds(1);
		var publisher = new Subject<int>();

		var list = Observable.ToObservable(publisher.ToAsyncEnumerable().ChunkForAddRange(maxInterval, fakeTimeProvider)).ToLiveList();

		foreach (var i in Enumerable.Range(1, 5)) {
			// 各要素の間で時間を進める
			fakeTimeProvider.Advance(maxInterval);
			// 要素追加
			publisher.OnNext(i);
			await WaitUntilAsync(() => list.SelectMany(x => x).Count(), i);
		}

		publisher.OnCompleted();
		await WaitUntilAsync(() => list.IsCompleted, true);

		// Assert
		// 各要素が別々のチャンクになるはず
		list.ShouldBe([[1], [2], [3], [4], [5]]);
		list.IsCompleted.ShouldBeTrue();
	}

	[Fact]
	public async Task ChunkForAddRange_ShouldGroupItemsInSameTimeInterval() {
		// Arrange
		var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
		var maxInterval = TimeSpan.FromSeconds(1);
		var publisher = new Subject<int>();

		var list = Observable.ToObservable(publisher.ToAsyncEnumerable().ChunkForAddRange(maxInterval, fakeTimeProvider)).ToLiveList();

		foreach (var i in Enumerable.Range(1, 5)) {
			// 要素追加
			publisher.OnNext(i);
			await Task.Delay(100);
		}
		publisher.OnCompleted();

		await WaitUntilAsync(() => list.IsCompleted, true);

		// Assert
		// すべての要素が1つのチャンクにまとめられるはず
		list.ShouldBe([[1, 2, 3, 4, 5]]);
		list.IsCompleted.ShouldBeTrue();
	}

	[Fact]
	public async Task ChunkForAddRange_ShouldHandleEmptySource() {
		// Arrange
		var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
		var maxInterval = TimeSpan.FromSeconds(1);
		var publisher = new Subject<int>();

		var list = Observable.ToObservable(publisher.ToAsyncEnumerable().ChunkForAddRange(maxInterval, fakeTimeProvider)).ToLiveList();

		// なにも追加せずに終了する。
		publisher.OnCompleted();

		await WaitUntilAsync(() => list.IsCompleted, true);

		// Assert
		// 空のソースからはチャンクが生成されないはず
		list.Count().ShouldBe(0);
		list.IsCompleted.ShouldBeTrue();
	}

	[Fact]
	public async Task ChunkForAddRange_ShouldSplitChunksOnSmallTimeAdvance() {
		// Arrange
		var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
		var maxInterval = TimeSpan.FromSeconds(1);
		var publisher = new Subject<int>();

		var list = Observable.ToObservable(publisher.ToAsyncEnumerable().ChunkForAddRange(maxInterval, fakeTimeProvider)).ToLiveList();

		foreach (var i in Enumerable.Range(1, 5)) {
			// 少し時間を進める。
			fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(400));
			// 要素追加
			publisher.OnNext(i);

			await Task.Delay(100);
		}
		publisher.OnCompleted();

		await WaitUntilAsync(() => list.IsCompleted, true);

		// Assert
		// 時間間隔が短いため2個のチャンクに分かれるはず
		list.ShouldBe([[1, 2, 3], [4, 5]]);
		list.IsCompleted.ShouldBeTrue();
	}

	#endregion

	#region ToUnit
	[Fact]
	public void ToUnit_ShouldConvertToUnit() {
		// Arrange
		var publisher = new Subject<int>();
		var results = new List<Unit>();

		// Act
		var subscription = publisher.ToUnit()
	  .Subscribe(results.Add);

		publisher.OnNext(1);
		publisher.OnNext(2);
		publisher.OnNext(3);
		publisher.OnCompleted();

		// Assert
		results.Count.ShouldBe(3);
		results.All(x => x == Unit.Default).ShouldBeTrue();
	}
	#endregion

	#region Throttle
	[Fact]
	public async Task Throttle_ShouldThrottleValues() {
		// Arrange
		var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

		var publisher = new Subject<int>();
		var results = new List<int>();

		// Act
		var subscription = publisher.Throttle(fakeTimeProvider).Subscribe(results.Add);

		// 連続して値を送信
		publisher.OnNext(1);
		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
		publisher.OnNext(2); // スロットルされるはず
		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
		publisher.OnNext(3);
		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
		publisher.OnNext(4);
		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
		publisher.OnNext(5); // スロットルされるはず
		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
		publisher.OnNext(6);
		// 十分な時間を空ける（300ms以上）
		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(400));
		publisher.OnNext(7);
		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
		publisher.OnNext(8); // スロットルされるはず
		publisher.OnCompleted();


		// Assert
		// 間引かれるはず
		results.ShouldBe([1, 3, 4, 6, 7]);
	}
	#endregion

	#region ToTwoWayBindableReactiveProperty
	[Fact]
	public void ToTwoWayBindableReactiveProperty_ShouldSyncBothWays() {
		// Arrange
		var source = new ReactiveProperty<int>(1);

		// Act
		var bindable = source.ToTwoWayBindableReactiveProperty(0);

		// Assert - Initial State
		bindable.Value.ShouldBe(1); // ソースの初期値が反映される

		// Assert - Source -> Bindable
		source.Value = 2;
		bindable.Value.ShouldBe(2);

		// Assert - Bindable -> Source
		bindable.Value = 3;
		source.Value.ShouldBe(3);
	}

	[Fact]
	public void ToTwoWayBindableReactiveProperty_ShouldUseSourceInitialValue() {
		// Arrange
		var source = new ReactiveProperty<int>(42); // 明示的な初期値

		// Act
		var bindable = source.ToTwoWayBindableReactiveProperty();

		// Assert
		bindable.Value.ShouldBe(42);
		source.Value.ShouldBe(42); // ソースにも反映される
	}

	[Fact]
	public void ToTwoWayBindableReactiveProperty_ShouldNotUseBindableInitialValue() {
		// Arrange
		var source = new ReactiveProperty<int>();

		// Act
		var bindable = source.ToTwoWayBindableReactiveProperty(42); // 明示的な初期値

		// Assert
		bindable.Value.ShouldBe(0); // ソースの初期値が優先される
		source.Value.ShouldBe(0); // ソースには反映されない。
	}
	#endregion
}