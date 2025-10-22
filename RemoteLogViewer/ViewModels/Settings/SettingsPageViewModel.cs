using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteLogViewer.ViewModels.Settings;

public abstract class SettingsPageViewModel : ViewModelBase {
	public string PageName {
		get;
	}

	public SettingsPageViewModel(string pageName) {
		this.PageName = pageName;
	}
}
