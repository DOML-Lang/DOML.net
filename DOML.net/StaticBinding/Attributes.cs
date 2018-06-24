#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;

namespace StaticBindings {
    /// <summary>
    /// Use this attribute to include a class/struct when generating from an assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class DOMLIncludeAttribute : Attribute {
        /// <summary>
        /// The root namespace that all objects exist in.
        /// </summary>
        public string RootNamespace { get; private set; }

        /// <summary>
        /// Create a new DOML include attribute customising the name.
        /// </summary>
        /// <param name="rootNamespace"> The root namespace that all objects exist in. </param>
        public DOMLIncludeAttribute(string rootNamespace) {
            this.RootNamespace = rootNamespace;
        }
    }

    /// <summary>
    /// Use this to customise a property/field/method (not constructor)/class/struct when used by DOML.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct | ~AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public class DOMLCustomiseAttribute : Attribute {
        /// <summary>
        /// The name to expose.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Create a new DOML customise attribute customising the name.
        /// </summary>
        /// <param name="name"> The name to customise. </param>
        public DOMLCustomiseAttribute(string name) {
            this.Name = name;
        }
    }

    /// <summary>
    /// Use this to ignore this particular field/property/method when building static bindings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DOMLIgnoreAttribute : Attribute {
    }
}
