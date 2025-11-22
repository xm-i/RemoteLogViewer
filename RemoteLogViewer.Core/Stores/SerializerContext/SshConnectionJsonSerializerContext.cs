using System.Text.Json.Serialization;
using RemoteLogViewer.Composition.Stores.Ssh;

namespace RemoteLogViewer.Core.Stores.SerializerContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SshConnectionProfileModelForJson))]
public partial class SshConnectionJsonSerializerContext : JsonSerializerContext {
}