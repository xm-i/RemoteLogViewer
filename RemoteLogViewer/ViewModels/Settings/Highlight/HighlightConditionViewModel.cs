using R3;

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

	public IReadOnlyBindableReactiveProperty<bool> IsForeColorSet {
		get;
	}

	public IReadOnlyBindableReactiveProperty<bool> IsBackColorSet {
		get;
	}

	public ReactiveCommand ClearForeColorCommand {
		get;
	} = new();

	public ReactiveCommand ClearBackColorCommand {
		get;
	} = new();

	public HighlightConditionViewModel(HighlightConditionModel model) {
		this.Model = model;

		this.Pattern = model.Pattern.ToTwoWayBindableReactiveProperty(string.Empty).AddTo(this.CompositeDisposable);
		this.PatternType = model.PatternType.ToTwoWayBindableReactiveProperty(HighlightPatternType.Exact).AddTo(this.CompositeDisposable);
		this.IgnoreCase = model.IgnoreCase.ToTwoWayBindableReactiveProperty(false).AddTo(this.CompositeDisposable);
		this.HighlightOnlyMatch = model.HighlightOnlyMatch.ToTwoWayBindableReactiveProperty(true).AddTo(this.CompositeDisposable);

		var defaultColor = Color.FromArgb(0x0, 0x0, 0x0, 0x0);
		this.ForeColor = model.ForeColor.Select(x => x.HasValue ? x.Value : defaultColor).ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.BackColor = model.BackColor.Select(x => x.HasValue ? x.Value : defaultColor).ToBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsForeColorSet = model.ForeColor.Select(x => x.HasValue).ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.IsBackColorSet = model.BackColor.Select(x => x.HasValue).ToReadOnlyBindableReactiveProperty().AddTo(this.CompositeDisposable);
		this.ForeColor.Subscribe(color => {
			if (this.Model.ForeColor.Value == null && color == defaultColor) {
				return;
			}
			this.Model.ForeColor.Value = color;
		}).AddTo(this.CompositeDisposable);
		this.BackColor.Subscribe(color => {
			if (this.Model.BackColor.Value == null && color == defaultColor) {
				return;
			}
			this.Model.BackColor.Value = color;
		}).AddTo(this.CompositeDisposable);
		this.ClearForeColorCommand.Subscribe(_ => this.Model.ForeColor.Value = null).AddTo(this.CompositeDisposable);
		this.ClearBackColorCommand.Subscribe(_ => this.Model.BackColor.Value = null).AddTo(this.CompositeDisposable);

	}
}