using System;

using Microsoft.Extensions.DependencyInjection;

using ObservableCollections;

using R3.JsonConfig.Attributes;

namespace RemoteLogViewer.Composition.Stores.Settings;

/// <summary>ハイライト設定。</summary>
[Inject(InjectServiceLifetime.Singleton)]
[GenerateR3JsonConfigDto]
public class HighlightSettingsModel(IServiceProvider service) {
	public IServiceProvider ScopedService { get; } = service;
	public ObservableList<HighlightRuleModel> Rules { get; } = [];

	public HighlightRuleModel AddRule() {
		var scope = this.ScopedService.CreateScope();
		var rule = scope.ServiceProvider.GetRequiredService<HighlightRuleModel>();
		this.Rules.Add(rule);
		return rule;
	}

	public void RemoveRule(HighlightRuleModel rule) {
		this.Rules.Remove(rule);
	}
}
