using System.Windows.Controls;

using RemoteLogViewer.Core.ViewModels.Info;

namespace RemoteLogViewer.WPF.Views.Info {
	public sealed partial class AboutPage : Page {
		public AboutPageViewModel? ViewModel {
			get;
			set;
		}

		public AboutPage() {
			this.InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e) {
			if (e.Parameter is AboutPageViewModel vm) {
				this.ViewModel = vm;
			}
			base.OnNavigatedTo(e);
		}
	}
}