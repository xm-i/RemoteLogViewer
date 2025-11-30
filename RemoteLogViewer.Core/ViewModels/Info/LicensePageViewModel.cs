using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace RemoteLogViewer.Core.ViewModels.Info;

[Inject(InjectServiceLifetime.Singleton)]
public class LicensePageViewModel : InfoPageViewModel<LicensePageViewModel> {
	public LicensePageViewModel(ILogger<LicensePageViewModel> logger) : base("Licenses", logger) {
	}

	public List<LicenseInfo> Licenses { get; } = [
		new("CommunityToolkit.Mvvm", "MIT", "https://github.com/CommunityToolkit/dotnet/blob/main/License.md"),
		new("CommunityToolkit.WinUI", "MIT", "https://github.com/CommunityToolkit/Windows/blob/main/License.md"),
		new("Microsoft.Extensions.DependencyInjection", "MIT", "https://github.com/dotnet/dotnet/blob/main/LICENSE.TXT"),
		new("Microsoft.WindowsAppSDK", "MIT", "https://github.com/microsoft/WindowsAppSDK/blob/main/LICENSE"),
		new("ObservableCollections", "MIT", "https://github.com/Cysharp/ObservableCollections/blob/master/LICENSE"),
		new("R3", "MIT", "https://github.com/Cysharp/R3/blob/main/LICENSE"),
		new("Serilog", "Apache-2.0", "https://github.com/serilog/serilog/blob/dev/LICENSE"),
		new("Split.js", "MIT", "https://github.com/nathancahill/split/blob/master/LICENSE"),
		new("SSH.NET", "MIT", "https://github.com/sshnet/SSH.NET/blob/develop/LICENSE"),
		new("System.Linq.Async", "MIT", "https://github.com/dotnet/reactive/blob/main/LICENSE"),
		new("Vue", "MIT", "https://github.com/vuejs/core/blob/main/LICENSE"),
	];
}

public record LicenseInfo(string Name, string License, string Url);
