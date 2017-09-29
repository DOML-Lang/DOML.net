using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DOML;
using DOML.IR;

namespace StaticBindings
{
    public static class CreateBindings
    {
        public static string DirectoryPath { get; set; }

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

        public static void Create<T>(string rootNamespace)
        {
            if (DirectoryPath == null)
                throw new NullReferenceException("Can't create bindings to a null directory path");

            CodeWriter codeWriter = new CodeWriter(DirectoryPath + "/StaticBinding-" + typeof(T).FullName + ".cs", false, rootNamespace);
            codeWriter.WriteHeader();
            codeWriter.WriteUsings();
            codeWriter.WriteBeginNamespace();
            codeWriter.WriteClass(typeof(T));
            codeWriter.WriteEndNamespace();
            codeWriter.Dispose();
        }
    }
}
