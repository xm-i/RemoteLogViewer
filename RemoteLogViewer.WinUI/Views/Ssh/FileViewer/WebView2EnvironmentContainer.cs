using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Microsoft.Web.WebView2.Core;

using RemoteLogViewer.Core.Services;

namespace RemoteLogViewer.WinUI.Views.Ssh.FileViewer;

[Inject(InjectServiceLifetime.Singleton)]
public class WebView2EnvironmentContainer {
	private readonly WorkspaceService _workspaceService;

	public CoreWebView2Environment? SharedEnvironment {
		get;
		private set;
	}

	public WebView2EnvironmentContainer(WorkspaceService workspaceService) {
		this._workspaceService = workspaceService;
	}

	[MemberNotNull(nameof(SharedEnvironment))]
	public async Task EnsureEnvironmentCreatedAsync() {
		var userData = this._workspaceService.GetConfigFilePath("WebView2");

		this.SharedEnvironment = null!;
		this.SharedEnvironment = await CoreWebView2Environment.CreateWithOptionsAsync(
			null,
			userData,
			new CoreWebView2EnvironmentOptions());
	}
}
