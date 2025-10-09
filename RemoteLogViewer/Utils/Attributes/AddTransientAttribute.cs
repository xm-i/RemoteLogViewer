namespace RemoteLogViewer.Utils.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AddTransientAttribute(Type? serviceType = null) : Attribute {
	public Type? ServiceType {
		get;
	} = serviceType;
}