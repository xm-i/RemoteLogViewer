using System.Text.Json.Serialization;

using RemoteLogViewer.Composition.Stores.Settings;
using RemoteLogViewer.Core.Stores.Converters;

namespace RemoteLogViewer.Core.Stores.SerializerContext;

[JsonSourceGenerationOptions(WriteIndented = true, Converters = [typeof(ColorJsonConverter)])]
[JsonSerializable(typeof(SettingsModelForJson))]
public partial class SettingsJsonSerializerContext : JsonSerializerContext {
}