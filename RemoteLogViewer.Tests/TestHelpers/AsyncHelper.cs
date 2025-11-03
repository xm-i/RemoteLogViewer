using System.Diagnostics;

namespace RemoteLogViewer.Tests.TestHelpers; 
internal class AsyncHelper {
	public static async Task WaitUntilAsync<T>(Func<T> valueFunc, T expected, int timeoutMs = 1000, int intervalMs = 5) {
		var sw = Stopwatch.StartNew();
		var value = valueFunc();
		while (!EqualityComparer<T>.Default.Equals(value, expected)) {
			if (sw.ElapsedMilliseconds > timeoutMs) {
				throw new TimeoutException($"Condition not met within timeout. value:{value} expected:{expected}");
			}
			await Task.Delay(intervalMs);
			value = valueFunc();
		}
	}
}
