using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;

public class LinuxOnlyFactAttribute : FactAttribute
{
    public LinuxOnlyFactAttribute()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Skip = "Only runs on Linux";
        }
    }
}