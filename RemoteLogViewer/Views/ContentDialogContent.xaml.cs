using Microsoft.UI.Xaml.Controls;

using RemoteLogViewer.Services;

namespace RemoteLogViewer.Views;

public sealed partial class ContentDialogContent : Page {
	public NotificationSeverity Severity {
		get;
	}

	public string MessageText {
		get;
	}

    public ContentDialogContent(string messageText, NotificationSeverity severity) {
		this.MessageText = messageText;
		this.Severity = severity;
		this.InitializeComponent();
    }
}
