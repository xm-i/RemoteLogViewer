namespace RemoteLogViewer.Utils.Extensions; 
public static class ObservableEx {
	public static Observable<Unit> ToUnit<T>(this Observable<T> source) {
		return source.Select(_ => Unit.Default);
	}

	public static Observable<T> Throttle<T>(this Observable<T> source) {
		return source.ThrottleFirstLast(TimeSpan.FromMilliseconds(300));
	}
}
