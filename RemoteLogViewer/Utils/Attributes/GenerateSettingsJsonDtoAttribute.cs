using R3.JsonConfig.Attributes;

namespace RemoteLogViewer.Utils.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class GenerateSettingsJsonDtoAttribute: GenerateR3JsonConfigDefaultDtoAttribute{
}
