#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;

namespace StaticBindings
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class DOMLIncludeAttribute : Attribute
    {
        public string RootNamespace { get; private set; }

        public DOMLIncludeAttribute(string rootNamespace)
        {
            this.RootNamespace = rootNamespace;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Class | ~AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public class DOMLCustomiseAttribute : Attribute
    {
        public string Name { get; private set; }

        public DOMLCustomiseAttribute(string name)
        {
            this.Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DOMLIgnoreAttribute : Attribute
    {
    }
}
