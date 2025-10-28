using Microsoft.Extensions.DependencyInjection;

using RemoteLogViewer.Stores.Settings.Model;

namespace RemoteLogViewer.ViewModels.Settings.Highlight;

[AddScoped]
public class HighlightRuleViewModel : ViewModelBase {
	public HighlightRuleModel Model {
		get;
	}

	public BindableReactiveProperty<string> Name {
		get;
	}

	public NotifyCollectionChangedSynchronizedViewList<HighlightConditionViewModel> Conditions {
		get;
	}

	public BindableReactiveProperty<HighlightConditionViewModel?> SelectedCondition {
		get;
	} = new();

	public ReactiveCommand AddConditionCommand {
		get;
	} = new();

	public ReactiveCommand<HighlightConditionViewModel> RemoveConditionCommand {
		get;
	} = new();

	public HighlightRuleViewModel(HighlightRuleModel model, IServiceProvider service) {
		this.Model = model;

		this.Name = model.Name.ToTwoWayBindableReactiveProperty("").AddTo(this.CompositeDisposable);
		var view = model.Conditions.CreateView(x => x.ScopedService.GetRequiredService<HighlightConditionViewModel>()).AddTo(this.CompositeDisposable);
		this.Conditions = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);

		this.AddConditionCommand.Subscribe(_ => {
			var condition = model.AddCondition();
			this.SelectedCondition.Value = condition.ScopedService.GetRequiredService<HighlightConditionViewModel>();
		}).AddTo(this.CompositeDisposable);

		this.RemoveConditionCommand.Subscribe(condition => {
			if (condition == null) {
				return;
			}
			model.RemoveCondition(condition.Model);
			if (this.SelectedCondition.Value == null) {
				this.SelectedCondition.Value = this.Conditions.LastOrDefault();
			}
		}).AddTo(this.CompositeDisposable);

		this.SelectedCondition.Value = this.Conditions.FirstOrDefault();
	}
}
