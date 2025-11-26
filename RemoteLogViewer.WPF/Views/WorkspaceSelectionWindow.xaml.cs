using System.Windows;

using Microsoft.Win32;

using RemoteLogViewer.Core.Services;
using RemoteLogViewer.Core.ViewModels.Settings;

namespace RemoteLogViewer.WPF.Views;

/// <summary>ワークスペース選択ウィンドウ。</summary>
[Inject(InjectServiceLifetime.Transient)]
public sealed partial class WorkspaceSelectionWindow : Window {
	/// <summary>選択イベント。(path, persist)</summary>
	public event Action? WorkspaceSelected;
	public WorkspaceSettingsPageViewModel ViewModel {
		get;
	}

	public WorkspaceSelectionWindow(WorkspaceSettingsPageViewModel vm) {
		this.InitializeComponent();
		this.ViewModel = vm;
		this.ViewModel.Confirmed += () => {
			this.WorkspaceSelected?.Invoke();
			this.Close();
		};
		this.ViewModel.ErrorMessage.ObservePropertyChanged(x => x.Value).Subscribe(msg => {
			if (!string.IsNullOrWhiteSpace(msg)) {
				var dialog = new ContentDialogWindow {
					MessageTitle = "エラー",
					PrimaryButtonText = "OK",
					PrimaryButtonCommand = new ReactiveCommand(_ => { }),
					Severity = NotificationSeverity.Error,
					Message = msg
				};
				dialog.ShowDialog();
			}
		});
	}

	private async void Browse_Click(object sender, RoutedEventArgs e) {
		var ofd = new OpenFolderDialog() {
			Multiselect = false
		};

		var result = ofd.ShowDialog();
		if (result ?? false) {
			this.ViewModel.SelectedPath.Value = ofd.FolderName;
		}
	}

	private void Ok_Click(object sender, RoutedEventArgs e) {
		this.ViewModel.ConfirmCommand.Execute(Unit.Default);
	}
}
