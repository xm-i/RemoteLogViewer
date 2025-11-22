using Microsoft.UI.Xaml;
using RemoteLogViewer.Core.ViewModels.Info;

namespace RemoteLogViewer.WinUI.Views.Info;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class InfoWindow : Window {
	public InfoWindowViewModel ViewModel {
		get;
	}

	public InfoWindow(InfoWindowViewModel vm) {
		this.ViewModel = vm;
		this.InitializeComponent();
		this.AppWindow?.Resize(new Windows.Graphics.SizeInt32(800, 800));
		this.ViewModel.SelectedSettingsPage.Subscribe(vm => {
			if (vm is null) {
				return;
			}
			Type view;
			switch (vm) {
				case LicensePageViewModel _:
					view = typeof(LicensePage);
					break;
				case AboutPageViewModel _:
					view = typeof(AboutPage);
					break;
				default:
					return;
			}

			this.ContentFrame.Navigate(view, vm);
		});
	}
}
