using Microsoft.UI.Xaml.Data;

namespace RemoteLogViewer.WinUI.Views.Converters;

/// <summary>
///     long 値を double に、double 値を long に相互変換します。
/// </summary>
public sealed class LongToDoubleConverter : IValueConverter {
	/// <summary>
	///     long から double へ変換します。
	/// </summary>
	/// <param name="value">long または数値文字列。</param>
	/// <param name="targetType">ターゲット型。</param>
	/// <param name="parameter">未使用。</param>
	/// <param name="language">未使用。</param>
	/// <returns>double 値。</returns>
	public object Convert(object value, Type targetType, object parameter, string language) {
		return value switch {
			long l => (double)l,
			int i => (double)i,
			string s when double.TryParse(s, out var d) => d,
			double d => d,
			_ => double.NaN
		};
	}

	/// <summary>
	///     double から long へ変換します。
	/// </summary>
	/// <param name="value">double または数値文字列。</param>
	/// <param name="targetType">ターゲット型。</param>
	/// <param name="parameter">未使用。</param>
	/// <param name="language">未使用。</param>
	/// <returns>long 値。</returns>
	public object ConvertBack(object value, Type targetType, object parameter, string language) {
		return value switch {
			double d => (long)Math.Round(d, MidpointRounding.AwayFromZero),
			float f => (long)Math.Round(f, MidpointRounding.AwayFromZero),
			string s when double.TryParse(s, out var d2) => (long)Math.Round(d2, MidpointRounding.AwayFromZero),
			long l => l,
			int i => (long)i,
			_ => 0L
		};
	}
}
