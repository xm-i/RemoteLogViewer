using System.Windows;
using System.Windows.Input;

using RemoteLogViewer.Core.Services;

namespace RemoteLogViewer.WPF.Views;

public sealed partial class ContentDialogWindow {
	public required string MessageTitle {
		get;
		init;
	}
	public required string Message {
		get;
		init;
	}
	public required NotificationSeverity Severity {
		get;
		init;
	}

	public required string PrimaryButtonText {
		get;
		init;
	}
	public required ICommand PrimaryButtonCommand {
		get;
		init;
	}

	public string? SecondaryButtonText {
		get;
		init;
	}
	public ICommand? SecondaryButtonCommand {
		get;
		init;
	}

	public Visibility HasPrimaryButton {
		get {
			return string.IsNullOrEmpty(this.PrimaryButtonText) ? Visibility.Collapsed : Visibility.Visible;
		}
	}

	public Visibility HasSecondaryButton {
		get {
			return string.IsNullOrEmpty(this.SecondaryButtonText) ? Visibility.Collapsed : Visibility.Visible;
		}
	}

	public ContentDialogWindow() {
		this.InitializeComponent();
		this.DataContext = this;
	}
}
