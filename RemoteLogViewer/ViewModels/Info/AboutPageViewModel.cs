using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.ViewModels.Info;

[AddSingleton]
public class AboutPageViewModel : InfoPageViewModel<AboutPageViewModel> {
	public AboutPageViewModel(ILogger<AboutPageViewModel> logger) : base("About", logger) {
	}

	public string AppName {
		get;
	} = "RemoteLogViewer";
	public string Version {
		get;
	} = "1.0.0";
	public string Description {
		get;
	} = "A Windows application for viewing remote log files over SSH.";
	public string Repository {
		get;
	} = "https://github.com/southernwind/RemoteLogViewer";
}