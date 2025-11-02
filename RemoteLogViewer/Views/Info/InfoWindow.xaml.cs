using Microsoft.UI.Xaml;

using RemoteLogViewer.ViewModels.Info;

namespace RemoteLogViewer.Views.Info;

[AddTransient]
public sealed partial class InfoWindow : Window {
	public InfoWindowViewModel ViewModel {
		get;
	}

	public InfoWindow(InfoWindowViewModel vm) {
		this.ViewModel = vm;
		this.InitializeComponent();
		this.AppWindow?.Resize(new Windows.Graphics.SizeInt32(1200, 800));
		this.ViewModel.SelectedSettingsPage.Subscribe(vm => {
			if (vm is null) {
				return;
			}
			Type view;
			switch (vm) {
				case LicensePageViewModel _:
					view = typeof(LicensePage);
					break;
				default:
					return;
			}

			this.ContentFrame.Navigate(view, vm);
		});
	}
}
