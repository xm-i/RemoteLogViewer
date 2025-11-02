using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RemoteLogViewer.ViewModels.Info;

namespace RemoteLogViewer.Views.Info {
	public sealed partial class LicensePage : Page {
		public LicensePageViewModel? ViewModel {
			get;
			set;
		}

		public LicensePage() {
			this.InitializeComponent();
			this.DataContext = this;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			if (e.Parameter is LicensePageViewModel vm) {
				this.ViewModel = vm;
			}
			base.OnNavigatedTo(e);
		}
	}
}