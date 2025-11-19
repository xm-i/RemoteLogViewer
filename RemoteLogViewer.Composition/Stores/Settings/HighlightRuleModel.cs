using System;

using Microsoft.Extensions.DependencyInjection;

using ObservableCollections;

using R3;
using R3.JsonConfig.Attributes;

namespace RemoteLogViewer.Composition.Stores.Settings;

/// <summary>ハイライトルール</summary>
[Inject(InjectServiceLifetime.Scoped)]
[GenerateR3JsonConfigDto]
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