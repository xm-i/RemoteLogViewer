using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using RemoteLogViewer.Services;
using RemoteLogViewer.ViewModels.Settings;
using RemoteLogViewer.ViewModels.Settings.Highlight;

using Windows.Storage.Pickers;

using WinRT.Interop;


namespace RemoteLogViewer.Views.Settings;

public sealed partial class WorkspaceSettingsPage : Page {
	public WorkspaceSettingsPageViewModel? ViewModel {
		get;
		private set;
	}

	public WorkspaceSettingsPage() {
		this.InitializeComponent();
	}
	/// <summary>
	/// ナビゲート時に ViewModel を受け取ります。
	/// </summary>
	protected override void OnNavigatedTo(NavigationEventArgs e) {
		if (e.Parameter is not WorkspaceSettingsPageViewModel vm) {
			throw new InvalidOperationException("ViewModel is not passed.");
		}
		this.ViewModel = vm;
		this.ViewModel.ErrorMessage.ObservePropertyChanged(x => x.Value).Subscribe(msg => {
			if (!string.IsNullOrWhiteSpace(msg)) {
				var dialog = new ContentDialog {
					XamlRoot = this.Content.XamlRoot,
					Title = "エラー",
					PrimaryButtonText = "OK",
					Content = msg
				};
				_ = dialog.ShowAsync();
			}
		});
		base.OnNavigatedTo(e);
	}

	private async void Browse_Click(object sender, RoutedEventArgs e) {
		if(this.ViewModel == null) {
			return;
		}
		var picker = new FolderPicker();
		picker.FileTypeFilter.Add("*");
		var window = Ioc.Default.GetRequiredService<SettingsWindow>();
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
		var folder = await picker.PickSingleFolderAsync();
		if (folder != null) {
			this.ViewModel.SelectedPath.Value = folder.Path;
		}
	}

	private void Ok_Click(object sender, RoutedEventArgs e) {
		if(this.ViewModel == null) {
			return;
		}
		this.ViewModel.ConfirmCommand.Execute(Unit.Default);
	}
}
