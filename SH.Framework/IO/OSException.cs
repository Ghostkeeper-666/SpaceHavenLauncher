namespace SH.Framework.IO;

using System;

public sealed class OSException : Exception
{
    public OSException() : base("Unknown operational system") { }
}