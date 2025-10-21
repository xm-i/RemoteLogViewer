using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RemoteLogViewer.Views.Settings;

public sealed partial class SettingsWindow : Window {
	public SettingsWindow() {
		this.InitializeComponent();
		this.CategoryList.SelectionChanged += CategoryList_SelectionChanged;
		this.CategoryList.SelectedIndex =0; // default select first
	}

	private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
		if (this.CategoryList.SelectedItem is not ListViewItem item) {
			return;
		}
		var key = item.Content as string;
		switch (key) {
			case "Highlight":
				this.ContentFrame.Navigate(typeof(HighlightSettingsPage));
				break;
			default:
				this.ContentFrame.Content = new TextBlock { Text = "Not implemented." };
				break;
		}
	}
}
