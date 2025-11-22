using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RemoteLogViewer.Core.Utils;

namespace RemoteLogViewer.Core.ViewModels.Info;

[Inject(InjectServiceLifetime.Singleton)]
public class InfoWindowViewModel : ViewModelBase<InfoWindowViewModel> {
	public ObservableList<string> Categories {
		get;
	} = [];

	public BindableReactiveProperty<IInfoPageViewModel> SelectedSettingsPage {
		get;
	} = new();

	public List<IInfoPageViewModel> Pages {
		get;
	} = [];

	public AboutPageViewModel AboutPageViewModel {
		get;
	}

	public LicensePageViewModel LicensePageViewModel {
		get;
	}

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public InfoWindowViewModel(AboutPageViewModel aboutPageViewModel, LicensePageViewModel licensePageViewModel, ILogger<InfoWindowViewModel> logger) : base(logger) {
		this.AboutPageViewModel = aboutPageViewModel;
		this.LicensePageViewModel = licensePageViewModel;

		this.Pages.AddRange([this.AboutPageViewModel, this.LicensePageViewModel]);
		this.SelectedSettingsPage.Value = this.Pages[0];
	}
}
