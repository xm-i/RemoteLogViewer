using Microsoft.Extensions.DependencyInjection;

namespace RemoteLogViewer.Stores.Settings.Model;

/// <summary>ハイライト設定。</summary>
[AddSingleton]
[GenerateSettingsJsonDto]
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
