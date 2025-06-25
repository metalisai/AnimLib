using System;

[AttributeUsage(AttributeTargets.Field)]
public class DynAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class GenerateDynPropertiesAttribute : Attribute
{
    public GenerateDynPropertiesAttribute(Type forType, bool onlyProperties = false)
    {
        
    }
}