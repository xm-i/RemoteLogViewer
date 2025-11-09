using Microsoft.CodeAnalysis;

using R3.JsonConfig.Generators;

namespace RemoteLogViewer.Generators;

[Generator]
public class SettingsJsonDtoGenerator : DefaultJsonDtoGenerator {
	protected override string TargetAttribute {
		get;
	} = "RemoteLogViewer.Utils.Attributes.GenerateConnectionJsonDtoAttribute";

	public SettingsJsonDtoGenerator() {
		this.ConversionRules.Add(new(
			"Windows.UI.Color",
			JsonDtoType.Text,
			"RemoteLogViewer.Utils.JsonUtils.ColorToHex",
			"RemoteLogViewer.Utils.JsonUtils.HexToColor"
		));
	}
}
