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
}
