using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>ハイライトルール</summary>
[AddScoped]
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

public class HighlightRuleModelForJson {
	public string? Name { get; set; } = string.Empty;
	public List<HighlightConditionModelForJson>? Conditions {
		get; set;
	}
	public static HighlightRuleModel CreateModel(HighlightRuleModelForJson json, IServiceProvider service) {
		var scope = service.CreateScope();
		var model = scope.ServiceProvider.GetRequiredService<HighlightRuleModel>();
		if (json.Name is { } name) {
			model.Name.Value = name;
		}
		if (json.Conditions is { } conditions) {
			foreach (var c in conditions) {
				model.Conditions.Add(HighlightConditionModelForJson.CreateModel(c, scope.ServiceProvider));
			}
		}
		return model;
	}
	public static HighlightRuleModelForJson CreateJson(HighlightRuleModel model) {
		return new HighlightRuleModelForJson {
			Name = model.Name.Value,
			Conditions = [.. model.Conditions.Select(HighlightConditionModelForJson.CreateJson)]
		};
	}
}
