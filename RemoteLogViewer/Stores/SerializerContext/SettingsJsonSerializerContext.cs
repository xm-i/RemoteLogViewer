using System.Text.Json.Serialization;

using RemoteLogViewer.Composition.Stores.Settings;
using RemoteLogViewer.Stores.Converters;

namespace RemoteLogViewer.Stores.SerializerContext;

[JsonSourceGenerationOptions(WriteIndented = true, Converters = [typeof(ColorJsonConverter)])]
[JsonSerializable(typeof(SettingsModelForJson))]
public partial class SettingsJsonSerializerContext : JsonSerializerContext {
}