using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI;

using Windows.UI;

namespace RemoteLogViewer.ViewModels.Settings.Highlight;

[AddScoped]
public class HighlightConditionViewModel {
	public BindableReactiveProperty<string> Pattern {
		get;
	} = new("");

	public BindableReactiveProperty<HighlightPatternType> PatternType {
		get;
	} = new(HighlightPatternType.Regex);

	public BindableReactiveProperty<bool> IgnoreCase {
		get;
	} = new(true);

	public BindableReactiveProperty<bool> HighlightOnlyMatch {
		get;
	} = new(false);

	public BindableReactiveProperty<Color> ForeColor {
		get;
	} = new(Colors.Black);

	public BindableReactiveProperty<Color> BackColor {
		get;
	} = new(Colors.Yellow);
}