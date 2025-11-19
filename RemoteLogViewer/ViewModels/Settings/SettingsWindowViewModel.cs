using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RemoteLogViewer.Stores.Settings;
using RemoteLogViewer.ViewModels.Settings.Highlight;

namespace RemoteLogViewer.ViewModels.Settings;

[Inject(InjectServiceLifetime.Singleton)]
public class SettingsWindowViewModel : ViewModelBase<SettingsWindowViewModel> {
	public ObservableList<string> Categories {
		get;
	} = [];

	public BindableReactiveProperty<ISettingsPageViewModel> SelectedSettingsPage {
		get;
	} = new();

	public List<ISettingsPageViewModel> Pages {
		get;
	} = [];

	public HighlightSettingsPageViewModel HighlightSettings {
		get;
	}

	public WorkspaceSettingsPageViewModel WorkspaceSettings {
		get;
	}
	public TextViewerSettingsPageViewModel TextViewerSettings {
		get;
	}
	public AdvancedSettingsPageViewModel AdvancedSettings {
		get;
	}

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public SettingsWindowViewModel(SettingsStoreModel model, WorkspaceSettingsPageViewModel workspaceSettings, TextViewerSettingsPageViewModel textViewerSettings, AdvancedSettingsPageViewModel advancedSettings, ILogger<SettingsWindowViewModel> logger) : base(logger) {
		this.HighlightSettings = model.SettingsModel.HighlightSettings.ScopedService.GetRequiredService<HighlightSettingsPageViewModel>();
		this.WorkspaceSettings = workspaceSettings;
		this.TextViewerSettings = textViewerSettings;
		this.AdvancedSettings = advancedSettings;

		this.Pages.AddRange([this.HighlightSettings, this.WorkspaceSettings, this.TextViewerSettings, this.AdvancedSettings]);
		this.SelectedSettingsPage.Value = this.Pages[0];

		this.SaveCommand.Subscribe(_ => {
			model.Save();
		});
	}
}
