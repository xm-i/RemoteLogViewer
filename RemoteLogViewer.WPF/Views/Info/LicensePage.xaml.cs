using System.Windows.Controls;

using RemoteLogViewer.Core.ViewModels.Info;

namespace RemoteLogViewer.WPF.Views.Info {
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