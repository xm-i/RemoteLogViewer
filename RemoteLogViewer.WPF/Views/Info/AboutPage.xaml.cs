using RemoteLogViewer.Core.ViewModels.Info;

namespace RemoteLogViewer.WPF.Views.Info {
	public sealed partial class AboutPage {
		public AboutPageViewModel? ViewModel {
			get;
			set;
		}

		public AboutPage() {
			this.InitializeComponent();
			this.DataContextChanged += (_, _2) => {
				if (this.DataContext is AboutPageViewModel vm) {
					this.ViewModel = vm;
				}
			};
		}
	}
}