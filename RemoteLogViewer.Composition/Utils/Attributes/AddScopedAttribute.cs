using System;

namespace RemoteLogViewer.Composition.Utils.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AddScopedAttribute(Type? serviceType = null) : Attribute {
	public Type? ServiceType {
		get;
	} = serviceType;
}