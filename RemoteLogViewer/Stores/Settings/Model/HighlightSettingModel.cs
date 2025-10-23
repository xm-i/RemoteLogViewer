using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>ハイライト設定。</summary>
[AddScoped]
public class HighlightSettingModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	public ObservableList<HighlightRuleModel> Rules { get; } = [];

	public HighlightRuleModel AddRule() {
		var scope = this.ScopedService.CreateScope();
		var rule = scope.ServiceProvider.GetRequiredService<HighlightRuleModel>();
		this.Rules.Add(rule);
		return rule;
	}

	public void removeRule(HighlightRuleModel rule) {
		this.Rules.Remove(rule);
	}
}

public class HighlightSettingModelForJson {
	public required List<HighlightRuleModelForJson> Rules {
		get; set;
	}

	public static HighlightSettingModel CreateModel(HighlightSettingModelForJson json, IServiceProvider service) {
		var scope = service.CreateScope();
		var model = scope.ServiceProvider.GetRequiredService<HighlightSettingModel>();
		if (json.Rules != null) {
			foreach (var c in json.Rules) {
				model.Rules.Add(HighlightRuleModelForJson.CreateModel(c, scope.ServiceProvider));
			}
		}
		return model;
	}
	public static HighlightSettingModelForJson CreateJson(HighlightSettingModel model) {
		return new HighlightSettingModelForJson {
			Rules = [.. model.Rules.Select(HighlightRuleModelForJson.CreateJson)]
		};
	}
}
