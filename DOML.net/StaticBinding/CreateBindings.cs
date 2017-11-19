#region License
// ====================================================
// Team DOML Copyright(C) 2017 Team DOML
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection; // While this dependency is 'fine' I would argue we should actively aim to not need it.

namespace StaticBindings
{
    /// <summary>
    /// Create Static Bindings.
    /// </summary>
    public static class CreateBindings
    {
        /// <summary>
        /// The directory path to create bindings to.
        /// </summary>
        public static string DirectoryPath { get; set; }

        /// <summary>
        /// The directory info of the path.
        /// </summary>
        public static DirectoryInfo Directory
        {
            get
            {
                return new DirectoryInfo(DirectoryPath);
            }
            set
            {
                DirectoryPath = value.FullName;
            }
        }

        /// <summary>
        /// The binding functions to hold together.
        /// So when it creates the final file it binds all the classes together.
        /// </summary>
        private readonly static List<string> BindingFunctions = new List<string>();

        /// <summary>
        /// Create using a generic overload.
        /// </summary>
        /// <typeparam name="T"> The type to create. </typeparam>
        /// <param name="rootNamespace"> The root namespace. </param>
        public static void Create<T>(string rootNamespace) => Create(typeof(T), rootNamespace);

        /// <summary>
        /// Create all from a class.
        /// </summary>
        /// <param name="forClass"> The type to create.</param>
        /// <param name="rootNamespace"> The root namespace. </param>
        public static void Create(Type forClass, string rootNamespace)
        {
            // Safety checks
            if (DirectoryPath == null)
                throw new NullReferenceException("Can't create bindings to a null directory path");

            using (CodeWriter codeWriter = new CodeWriter(DirectoryPath + "/StaticBinding-" + forClass.FullName + ".cs", false, rootNamespace))
            {
                codeWriter.WriteSuppression("IDE0012", true, "Ignorning warning for shorter names");
                codeWriter.WriteHeader();
                codeWriter.WriteEmptyLine();

                codeWriter.WriteUsings();
                codeWriter.WriteEmptyLine();

                codeWriter.WriteNamespaceSignature("StaticBindings.BindingsFor");
                codeWriter.WriteClass(forClass);
                codeWriter.CloseBrace();

                codeWriter.WriteSuppression("IDE0012", false, "Restore warning for shorter names");
            }

            BindingFunctions.Add($"StaticBindings.BindingsFor{rootNamespace}.____{forClass.Name}StaticBindings____");
        }

        /// <summary>
        /// Create all that use the <see cref="DOMLIncludeAttribute"/> in assembly provided.
        /// </summary>
        /// <param name="assemblyName"> The assembly to search in. </param>
        /// <param name="withFinalFile"> Create the final binding file. </param>
        public static void CreateAllFromAttributes(string assemblyName, bool withFinalFile = true) => CreateAllFromAttributes(Assembly.Load(new AssemblyName(assemblyName)), withFinalFile);

        /// <summary>
        /// Create all that use the <see cref="DOMLIncludeAttribute"/> in assembly provided.
        /// </summary>
        /// <param name="assembly"> The assembly to search in. </param>
        /// <param name="withFinalFile"> Create the final binding file. </param>
        public static void CreateAllFromAttributes(Assembly assembly, bool withFinalFile = true)
        {
            IEnumerable<TypeInfo> classesAndStructs = assembly.DefinedTypes.Where(x => (x.IsClass || x.IsValueType) && x.GetCustomAttribute(typeof(DOMLIncludeAttribute)) != null);
            foreach (TypeInfo type in classesAndStructs)
            {
                Create(type.AsType(), type.GetCustomAttribute<DOMLIncludeAttribute>().RootNamespace);
            }

            if (withFinalFile) CreateFinalFile();
        }

        /// <summary>
        /// Create the final file to bind all the classes together.
        /// </summary>
        public static void CreateFinalFile()
        {
            // Safety checks
            if (DirectoryPath == null)
                throw new NullReferenceException("Can't create bindings to a null directory path.");

            if (BindingFunctions == null)
                throw new NullReferenceException("No classes created during the static binding operation.");

            using (CodeWriter codeWriter = new CodeWriter(DirectoryPath + "/StaticBindingRegister.cs", false, null))
            {
                codeWriter.WriteHeader();
                codeWriter.WriteUsings();
                codeWriter.WriteEmptyLine();

                codeWriter.WriteClassSignature("DOMLBindings", false);

                codeWriter.WriteFunctionSignature("LinkBindings", string.Empty);
                for (int i = 0; i < BindingFunctions.Count; i++)
                {
                    if (i > 0) codeWriter.WriteEmptyLine();

                    codeWriter.WriteIndentLine($"{BindingFunctions[i]}.RegisterCalls();");
                }

                codeWriter.CloseBrace();
                codeWriter.WriteEmptyLine();

                codeWriter.WriteFunctionSignature("UnLinkBindings", string.Empty);
                for (int i = 0; i < BindingFunctions.Count; i++)
                {
                    codeWriter.WriteIndentLine($"{BindingFunctions[i]}.UnRegisterCalls();");
                }

                codeWriter.CloseBrace();
                codeWriter.CloseBrace();
            }

            BindingFunctions.Clear();
        }
    }
}
