namespace RemoteLogViewer.Utils.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AddSingletonAttribute(Type? serviceType = null) : Attribute {
	public Type? ServiceType {
		get;
	} = serviceType;
}