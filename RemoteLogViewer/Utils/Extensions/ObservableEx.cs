using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RemoteLogViewer.Utils.Extensions;

public static class ObservableEx {
	public static Observable<Unit> ToUnit<T>(this Observable<T> source) {
		return source.Select(_ => Unit.Default);
	}

	public static Observable<T> Throttle<T>(this Observable<T> source) {
		return source.ThrottleFirstLast(TimeSpan.FromMilliseconds(300));
	}
	public static BindableReactiveProperty<T> ToTwoWayBindableReactiveProperty<T>(this ReactiveProperty<T> source, T initialValue = default!) {
		var bindable = source.ToBindableReactiveProperty(initialValue);
		bindable.Subscribe(x => {
			source.Value = x;
		});
		return bindable;
	}

	public static async IAsyncEnumerable<IEnumerable<T>> ChunkForAddRange<T>(
		this IAsyncEnumerable<T> source,
		TimeSpan maxInterval,
		TimeProvider? timeProvider = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default) {
		if (timeProvider == null) {
			timeProvider = TimeProvider.System;
		}
		var buffer = new List<T>();
		var lastFlush = timeProvider.GetUtcNow();

		await foreach (var item in source.WithCancellation(cancellationToken)) {
			buffer.Add(item);

			var now = timeProvider.GetUtcNow();
			if (now - lastFlush >= maxInterval) {
				yield return buffer;
				buffer.Clear();
				lastFlush = now;
			}
		}

		if (buffer.Count > 0) {
			yield return buffer;
		}
	}
}
