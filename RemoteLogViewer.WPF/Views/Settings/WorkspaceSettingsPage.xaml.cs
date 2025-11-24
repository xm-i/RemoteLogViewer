using System.Windows;

using Microsoft.Win32;

using RemoteLogViewer.Core.Services;
using RemoteLogViewer.Core.ViewModels.Settings;

namespace RemoteLogViewer.WPF.Views.Settings;

public sealed partial class WorkspaceSettingsPage {
	public WorkspaceSettingsPageViewModel? ViewModel {
		get;
		private set;
	}

	public WorkspaceSettingsPage() {
		this.InitializeComponent();
		this.DataContextChanged += (_, _2) => {
			if (this.DataContext is WorkspaceSettingsPageViewModel vm) {
				this.ViewModel = vm;
				this.ViewModel.ErrorMessage.ObservePropertyChanged(x => x.Value).Subscribe(msg => {
					if (!string.IsNullOrWhiteSpace(msg)) {
						var dialog = new ContentDialogWindow {
							MessageTitle = "エラー",
							PrimaryButtonText = "OK",
							PrimaryButtonCommand = new ReactiveCommand(),
							Message = msg,
							Severity = NotificationSeverity.Error
						};
						dialog.ShowDialog();
					}
				});
			}
		};
	}

	private async void Browse_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		var ofd = new OpenFolderDialog() {
			Multiselect = false
		};

		var result = ofd.ShowDialog();
		if (result ?? false) {
			this.ViewModel.SelectedPath.Value = ofd.FolderName;
		}
	}

	private void Ok_Click(object sender, RoutedEventArgs e) {
		if (this.ViewModel == null) {
			return;
		}
		this.ViewModel.ConfirmCommand.Execute(Unit.Default);
	}
}
