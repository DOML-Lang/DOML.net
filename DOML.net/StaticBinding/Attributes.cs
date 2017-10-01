using System;
using System.Collections.Generic;
using System.Text;

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
