using System.Collections.Generic;

using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.ViewModels.Info;

[AddSingleton]
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

	public LicensePageViewModel LicensePageViewModel {
		get;
	}

	public ReactiveCommand SaveCommand {
		get;
	} = new();

	public InfoWindowViewModel(LicensePageViewModel licensePageViewModel, ILogger<InfoWindowViewModel> logger) : base(logger) {
		this.LicensePageViewModel = licensePageViewModel;

		this.Pages.AddRange([this.LicensePageViewModel]);
		this.SelectedSettingsPage.Value = this.Pages[0];
	}
}
