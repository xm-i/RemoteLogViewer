using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>ハイライトルール</summary>
[AddScoped]
[GenerateSettingsJsonDto]
public class HighlightRuleModel(IServiceProvider service) {
	public IServiceProvider ScopedService {
		get;
	} = service;

	public ReactiveProperty<string> Name { get; } = new("New Item");
	public ObservableList<HighlightConditionModel> Conditions { get; } = [];

	public HighlightConditionModel AddCondition() {
		var scope = this.ScopedService.CreateScope();
		var condition = scope.ServiceProvider.GetRequiredService<HighlightConditionModel>();
		this.Conditions.Add(condition);
		return condition;
	}

	public void RemoveCondition(HighlightConditionModel condition) {
		this.Conditions.Remove(condition);
	}
}