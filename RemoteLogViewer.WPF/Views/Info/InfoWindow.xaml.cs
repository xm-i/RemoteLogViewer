using System.Windows;

using RemoteLogViewer.Core.ViewModels.Info;

namespace RemoteLogViewer.WPF.Views.Info;

[Inject(InjectServiceLifetime.Transient)]
public sealed partial class InfoWindow : Window {
	public InfoWindowViewModel ViewModel {
		get;
	}

	public InfoWindow(InfoWindowViewModel vm) {
		this.ViewModel = vm;
		this.DataContext = vm;
		this.InitializeComponent();
	}
}
