using Microsoft.Extensions.DependencyInjection;

using System.Collections.Generic;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>ハイライト設定。</summary>
[AddSingleton]
public class HighlightSettingsModel(IServiceProvider service) {
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

public class HighlightSettingsModelForJson {
	public required List<HighlightRuleModelForJson> Rules {
		get; set;
	}

	public static HighlightSettingsModel CreateModel(HighlightSettingsModelForJson json, IServiceProvider service) {
		var model = service.GetRequiredService<HighlightSettingsModel>();
		if (json.Rules != null) {
			foreach (var c in json.Rules) {
				model.Rules.Add(HighlightRuleModelForJson.CreateModel(c, service));
			}
		}
		return model;
	}
	public static HighlightSettingsModelForJson CreateJson(HighlightSettingsModel model) {
		return new HighlightSettingsModelForJson {
			Rules = [.. model.Rules.Select(HighlightRuleModelForJson.CreateJson)]
		};
	}
}
