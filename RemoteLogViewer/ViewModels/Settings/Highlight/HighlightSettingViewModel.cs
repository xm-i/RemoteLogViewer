using System.Linq;

using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.ViewModels.Settings.Highlight;

[AddScoped]
public class HighlightSettingViewModel: ViewModelBase {
	private readonly ObservableList<HighlightConditionViewModel> _conditions = [];
	public BindableReactiveProperty<string> Name {
		get;
	} = new("New Item");

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

	public HighlightSettingViewModel(IServiceProvider service) {
		// View生成
		var view = this._conditions.CreateView(x => x).AddTo(this.CompositeDisposable);
		this.Conditions = view.ToNotifyCollectionChanged().AddTo(this.CompositeDisposable);

		this.AddConditionCommand.Subscribe(_ => {
			var scope = service.CreateScope();
			var cond = scope.ServiceProvider.GetRequiredService<HighlightConditionViewModel>();
			this._conditions.Add(cond);
			this.SelectedCondition.Value = cond;
		});

		this.RemoveConditionCommand.Subscribe(conditionObj => {
			if (conditionObj == null) {
				return;
			}
			this._conditions.Remove(conditionObj);
			if (this.SelectedCondition.Value == conditionObj) {
				this.SelectedCondition.Value = this._conditions.LastOrDefault();
			}
		});
	}
}
