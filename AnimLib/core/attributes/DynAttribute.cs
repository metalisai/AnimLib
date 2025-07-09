using System;

[AttributeUsage(AttributeTargets.Field)]
public class DynAttribute : Attribute
{
    public DynAttribute(string[]? onSet = null)
    {
        
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class GenerateDynPropertiesAttribute : Attribute
{
    public GenerateDynPropertiesAttribute(Type forType, bool onlyProperties = false)
    {
        
    }
}