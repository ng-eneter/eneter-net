/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if COMPACT_FRAMEWORK

namespace System.Diagnostics
{
    internal enum DebuggerBrowsableState
    {
        RootHidden = 3
    }
    internal class DebuggerBrowsableAttribute : Attribute
    {
        public DebuggerBrowsableAttribute(DebuggerBrowsableState state)
        {
        }
    }
    
    internal class DebuggerDisplayAttribute : Attribute
    {
        public DebuggerDisplayAttribute(string value)
        {
        }
    }
    
    internal class DebuggerTypeProxyAttribute : Attribute
    {
        public DebuggerTypeProxyAttribute(Type type)
        {
        }
    }
}

namespace System.Runtime.Serialization
{
	internal class DataContract : Attribute
	{
	}
	
	internal class DataMember : Attribute
	{
	}
}

namespace System.Reflection
{
    internal class AssemblyFileVersionAttribute : Attribute
    {
        public AssemblyFileVersionAttribute(string version)
        {
        }
    }
}

#endif

#if COMPACT_FRAMEWORK20

namespace System
{
	internal class SerializableAttribute : Attribute
	{
	}
}
	

namespace System.Runtime.CompilerServices
{
    internal class CompilerGeneratedAttribute : Attribute
    {
    }
}

#endif
