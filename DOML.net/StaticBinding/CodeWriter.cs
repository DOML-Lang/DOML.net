using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace StaticBindings
{
    public class CodeWriter : IDisposable
    {
        private TextWriter writer;
        private string rootNamespace;
        private List<string> registerCalls = new List<string>();
        private List<string> unregisterCalls = new List<string>();
        private int indentLevel = 0;

        public CodeWriter(string filePath, bool append, string rootNamespace)
            : this(new StreamWriter(File.Exists(filePath) ? File.Open(filePath, FileMode.Truncate) : new FileStream(filePath, append ? FileMode.Append : FileMode.Create)),
                  rootNamespace)
        {
        }

        public CodeWriter(StringBuilder resultText, string rootNamespace) : this(new StringWriter(resultText), rootNamespace)
        {
        }

        private CodeWriter(TextWriter writer, string rootNamespace)
        {
            this.writer = writer;
            this.rootNamespace = rootNamespace;
        }

        ~CodeWriter()
        {
            Finish();
        }

        public void Dispose()
        {
            Finish();
        }

        public void WriteLine(string text) => writer.WriteLine(text);

        public void Write(string text) => writer.Write(text);

        public void WriteEmptyLine() => writer.WriteLine();

        public void WriteIndentLine(string text) => writer.WriteLine(new string('\t', indentLevel) + text);

        public void WriteWithIndent(string text) => writer.Write(new string('\t', indentLevel) + text);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="disable"> If true it'll disable the suppression else restore it. </param>
        public void WriteSuppression(string code, bool disable, string comment)
        {
            WriteLine($"#pragma warning {(disable ? "disable" : "restore")} {code} // {comment}");
        }

        public void WriteHeader()
        {
            Write("/* THIS IS AUTO-GENERATED\n * ALL CHANGES WILL BE RESET\n * UPON GENERATION\n */\n");
        }

        public void WriteUsings()
        {
            WriteLine("using DOML.Logger;");
            WriteLine("using DOML.IR;");
        }

        public void WriteNamespaceSignature()
        {
            WriteIndentLine($"namespace {rootNamespace}");
            WriteIndentLine("{");
            indentLevel++;
        }

        public void CloseBrace()
        {
            indentLevel--;
            WriteIndentLine("}");
        }

        public void WriteClass(Type classType)
        {
            IEnumerable<FieldInfo> fieldInfo = classType.GetRuntimeFields();
            IEnumerable<PropertyInfo> propertyInfo = classType.GetRuntimeProperties();
            IEnumerable<MethodInfo> methodInfo = classType.GetRuntimeMethods();
            string objectType = classType.GetTypeInfo().GetCustomAttribute<DOMLCustomiseAttribute>()?.Name ?? classType.Name;

            WriteClassSignature(classType.Name);
            bool notInitial = false;

            foreach (FieldInfo info in fieldInfo.Where(x => x.DeclaringType == classType))
            {
                if (notInitial) WriteEmptyLine();
                else notInitial = true;

                WriteField(info, objectType);
            }

            foreach (PropertyInfo info in propertyInfo.Where(x => x.DeclaringType == classType))
            {
                if (notInitial) WriteEmptyLine();
                else notInitial = true;

                WriteProperty(info, objectType);
            }

            foreach (MethodInfo info in methodInfo.Where(x => x.DeclaringType == classType))
            {
                if (notInitial) WriteEmptyLine();
                else notInitial = true;

                WriteFunction(info, objectType);
            }

            if (notInitial == false)
            {
                throw new ArgumentException("Class requires atleast one usable method/property/field");
            }

            WriteEmptyLine();

            WriteRegisterCall();

            WriteEmptyLine();

            WriteUnRegisterCall();
            CloseBrace();
        }

        public void WriteRegisterCall()
        {
            WriteFunctionSignature("RegisterCalls", string.Empty);
            for (int i = 0; i < registerCalls.Count; i++)
            {
                WriteIndentLine(registerCalls[i]);
            }
            CloseBrace();
        }

        public void WriteUnRegisterCall()
        {
            WriteFunctionSignature("UnRegisterCalls", string.Empty);
            for (int i = 0; i < unregisterCalls.Count; i++)
            {
                WriteIndentLine(unregisterCalls[i]);
            }
            CloseBrace();
        }

        public void WriteClassSignature(string name, bool decorator = true)
        {
            WriteIndentLine($"public static partial class {(decorator ? "____" : string.Empty)}{name}{(decorator ? "StaticBindings____" : string.Empty)}");
            WriteIndentLine("{");
            indentLevel++;
        }

        public void WriteFunctionSignature(string name, string parameters)
        {
            WriteIndentLine($"public static void {name}({parameters})");
            WriteIndentLine("{");
            indentLevel++;
        }

        public void WriteFunction(MethodInfo methodInfo, string objectType)
        {
            ParameterInfo[] info = methodInfo.GetParameters();
            string type = methodInfo.IsConstructor ? "New" : (methodInfo.ReturnType == typeof(void) ? "Set" : "Get");
            string functionName = type + methodInfo.Name;
            WriteFunctionSignature(functionName, "InterpreterRuntime runtime");

            if (methodInfo.IsConstructor == false)
                WriteWithIndent($"if (!runtime.Pop(out {methodInfo.DeclaringType.FullName} result)");
            else
                WriteWithIndent($"if (");

            for (int i = 0; i < info.Length; i++)
            {
                Write($" || !runtime.Pop(out {info[i].ParameterType.FullName} a{i})");
            }

            if (methodInfo.IsConstructor)
            {
                if (info.Length > 0)
                    Write($"!runtime.Push(new {methodInfo.Name}({string.Join(",", Enumerable.Range(0, info.Length).Select(x => "a" + x))}), true)");
                else
                    Write($"!runtime.Push(new {methodInfo.Name}(), true)");
            }
            else if (methodInfo.ReturnType != typeof(void))
            {
                if (info.Length > 0)
                    Write($" || !runtime.Push(result.{methodInfo.Name}({string.Join(",", Enumerable.Range(0, info.Length).Select(x => "a" + x))}), true)");
                else
                    Write($" || !runtime.Push(result.{methodInfo.Name}(), true)");
            }
            WriteLine(")");

            indentLevel++;

            WriteIndentLine($"Log.Error(\"{objectType}::{methodInfo.Name} Function failed\");");

            indentLevel--;

            if (methodInfo.IsConstructor == false && methodInfo.ReturnType == typeof(void))
            {
                WriteIndentLine("else");
                indentLevel++;

                if (info.Length > 0)
                    WriteIndentLine($"result.{methodInfo.Name}({string.Join(", ", Enumerable.Range(0, info.Length).Select(x => "a" + x))});");
                else
                    WriteIndentLine($"result.{methodInfo.Name}();");

                indentLevel--;
            }
            CloseBrace();

            string name = $"{methodInfo.GetCustomAttribute<DOMLCustomiseAttribute>()?.Name ?? methodInfo.Name}";

            switch (type)
            {
            case "New":
                registerCalls.Add($"InstructionRegister.RegisterConstructor(\"{rootNamespace}.{objectType}\", {functionName});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterConstructor(\"{rootNamespace}.{objectType}\");");
                break;
            case "Get":
                registerCalls.Add($"InstructionRegister.RegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\", 1, {functionName});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
                break;
            case "Set":
                registerCalls.Add($"InstructionRegister.RegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\", {info.Length}, {functionName});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
                break;
            default:
                throw new NotImplementedException("Type not implemented");
            }
        }

        public void WriteProperty(PropertyInfo propertyInfo, string objectType)
        {
            string name = $"{propertyInfo.GetCustomAttribute<DOMLCustomiseAttribute>()?.Name ?? propertyInfo.Name}";

            if (propertyInfo.CanRead && (propertyInfo.GetMethod?.IsPublic ?? false))
            {
                // Write Getter
                WriteFunctionSignature($"GetProperty{propertyInfo.Name}", "InterpreterRuntime runtime");
                WriteIndentLine($"if (!runtime.Pop(out {propertyInfo.DeclaringType.Name} result) || !runtime.Push(result.{propertyInfo.Name}, true))");
                indentLevel++;
                WriteIndentLine($"Log.Error(\"{objectType}::{name} Getter failed\");");
                indentLevel--;
                CloseBrace();

                registerCalls.Add($"InstructionRegister.RegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, GetProperty{propertyInfo.Name});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
            }

            if (propertyInfo.CanWrite && (propertyInfo.SetMethod?.IsPublic ?? false))
            {
                WriteEmptyLine();

                // Write Setter
                WriteFunctionSignature($"SetProperty{propertyInfo.Name}", "InterpreterRuntime runtime");
                WriteIndentLine($"if (runtime.Pop(out {propertyInfo.DeclaringType.FullName} result) || !runtime.Pop(out result.{propertyInfo.Name}))");
                indentLevel++;
                WriteIndentLine($"Log.Error(\"{objectType}::{name} Setter failed\");");
                indentLevel--;
                CloseBrace();

                registerCalls.Add($"InstructionRegister.RegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, SetProperty{propertyInfo.Name});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
            }
        }

        public void WriteField(FieldInfo fieldInfo, string objectType)
        {
            string name = $"{fieldInfo.GetCustomAttribute<DOMLCustomiseAttribute>()?.Name ?? fieldInfo.Name}";

            // Write Getter
            WriteFunctionSignature($"GetField{name}", "InterpreterRuntime runtime");
            WriteIndentLine($"if (!runtime.Pop(out {fieldInfo.DeclaringType.FullName} result) || !runtime.Push(result.{fieldInfo.Name}, true))");
            indentLevel++;
            WriteIndentLine($"Log.Error(\"{objectType}::{name} Getter failed\");");
            indentLevel--;
            CloseBrace();

            registerCalls.Add($"InstructionRegister.RegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, GetField{fieldInfo.Name});");
            unregisterCalls.Add($"InstructionRegister.UnRegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\");");

            if (fieldInfo.IsLiteral == false && fieldInfo.IsInitOnly == false)
            {
                WriteEmptyLine();

                // Write Setter
                WriteFunctionSignature($"SetField{fieldInfo.Name}", "InterpreterRuntime runtime");
                WriteIndentLine($"if (runtime.Pop(out {fieldInfo.DeclaringType.FullName} result) || !runtime.Pop(out result.{fieldInfo.Name}))");
                indentLevel++;
                WriteIndentLine($"Log.Error(\"{objectType}::{name} Setter failed\");");
                indentLevel--;
                CloseBrace();

                registerCalls.Add($"InstructionRegister.RegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, SetField{fieldInfo.Name});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
            }
        }

        private void Finish()
        {
            writer.Dispose();
        }
    }
}
