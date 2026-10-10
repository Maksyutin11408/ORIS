namespace CustomHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ControllerAttribute : Attribute
{
    public string Name { get; }
    public ControllerAttribute(string name) => Name = name;
}