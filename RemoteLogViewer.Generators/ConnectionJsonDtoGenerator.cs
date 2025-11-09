using Microsoft.CodeAnalysis;

using R3.JsonConfig.Generators;

namespace RemoteLogViewer.Generators;

[Generator]
public class ConnectionJsonDtoGenerator : DefaultJsonDtoGenerator {
	protected override string TargetAttribute {
		get;
	} = "RemoteLogViewer.Utils.Attributes.GenerateSettingsJsonDtoAttribute";

	public ConnectionJsonDtoGenerator() {
		this.ConversionRules.Add(new(
			"System.Guid",
			JsonDtoType.Text,
			"RemoteLogViewer.Utils.JsonUtils.GuidToString",
			"System.Guid.Parse"
		));
	}
}
