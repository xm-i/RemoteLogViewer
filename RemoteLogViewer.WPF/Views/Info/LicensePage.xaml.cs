using System.Windows.Documents;

using RemoteLogViewer.Core.ViewModels.Info;

namespace RemoteLogViewer.WPF.Views.Info {
	public sealed partial class LicensePage {
		public LicensePageViewModel? ViewModel {
			get;
			set;
		}

		public LicensePage() {
			this.InitializeComponent();
			this.DataContextChanged += (_, _2) => {
				if (this.DataContext is LicensePageViewModel vm) {
					this.ViewModel = vm;
				}
			};
		}

		private void Hyperlink_Click(object sender, System.Windows.RoutedEventArgs e) {
			if (sender is Hyperlink link) {

			}
		}
	}
}