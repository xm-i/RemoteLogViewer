using RemoteLogViewer.Stores.Settings.Model;

using Windows.UI;

namespace RemoteLogViewer.ViewModels.Settings.Highlight;

[AddScoped]
public class HighlightConditionViewModel : ViewModelBase {
	public HighlightConditionModel Model {
		get;
	}

	public BindableReactiveProperty<string> Pattern {
		get;
	}

	public BindableReactiveProperty<HighlightPatternType> PatternType {
		get;
	}

	public BindableReactiveProperty<bool> IgnoreCase {
		get;
	}

	public BindableReactiveProperty<bool> HighlightOnlyMatch {
		get;
	}

	public BindableReactiveProperty<Color> ForeColor {
		get;
	}

	public BindableReactiveProperty<Color> BackColor {
		get;
	}

	public HighlightConditionViewModel(HighlightConditionModel model) {
		this.Model = model;

		this.Pattern = model.Pattern.ToBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.PatternType = model.PatternType.ToBindableReactiveProperty(HighlightPatternType.Exact).AddTo(this.CompositeDisposable);
		this.IgnoreCase = model.IgnoreCase.ToBindableReactiveProperty(false).AddTo(this.CompositeDisposable);
		this.HighlightOnlyMatch = model.HighlightOnlyMatch.ToBindableReactiveProperty(true).AddTo(this.CompositeDisposable);
		this.ForeColor = model.ForeColor.ToBindableReactiveProperty(Color.FromArgb(0, 0, 0, 0)).AddTo(this.CompositeDisposable);
		this.BackColor = model.BackColor.ToBindableReactiveProperty(Color.FromArgb(255, 255, 255, 0)).AddTo(this.CompositeDisposable);
	}
}