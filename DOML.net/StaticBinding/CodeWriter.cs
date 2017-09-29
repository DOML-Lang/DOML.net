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

        public void WriteHeader()
        {
            writer.Write("/* THIS IS AUTO-GENERATED\n * ALL CHANGES WILL BE RESET\n * UPON GENERATION\n */\n");
        }

        public void WriteUsings()
        {
            writer.WriteLine("using DOML.Logger;");
            writer.WriteLine("using DOML.IR;");
        }

        public void WriteBeginNamespace()
        {
            writer.WriteLine("namespace UserStaticBindings {");
        }

        public void WriteEndNamespace()
        {
            writer.WriteLine("}");
        }

        public void WriteClass(Type classType)
        {
            IEnumerable<FieldInfo> fieldInfo = classType.GetRuntimeFields();
            IEnumerable<PropertyInfo> propertyInfo = classType.GetRuntimeProperties();
            IEnumerable<MethodInfo> methodInfo = classType.GetRuntimeMethods();
            string objectType = classType.GetTypeInfo().GetCustomAttribute<DOMLCustomiseAttribute>()?.Name ?? classType.Name;

            WriteClassBegin(classType);

            foreach (FieldInfo info in fieldInfo.Where(x => x.DeclaringType == classType))
            {
                WriteField(info, objectType);
            }

            foreach (PropertyInfo info in propertyInfo.Where(x => x.DeclaringType == classType))
            {
                WriteProperty(info, objectType);
            }

            foreach (MethodInfo info in methodInfo.Where(x => x.DeclaringType == classType))
            {
                WriteFunction(info, objectType);
            }

            WriteRegisterCall();
            WriteUnRegisterCall();
            WriteClassEnd();
        }

        private void WriteRegisterCall()
        {
            writer.WriteLine("public void RegisterCalls() {");
            for (int i = 0; i < registerCalls.Count; i++)
            {
                writer.WriteLine(registerCalls[i]);
            }
            writer.WriteLine("}");
        }

        private void WriteUnRegisterCall()
        {
            writer.WriteLine("public void UnRegisterCalls() {");
            for (int i = 0; i < unregisterCalls.Count; i++)
            {
                writer.WriteLine(unregisterCalls[i]);
            }
            writer.WriteLine("}");
        }

        private void WriteClassBegin(Type classType)
        {
            writer.WriteLine($"public class ____{classType.Name}StaticBindings____ {{");
        }

        private void WriteClassEnd()
        {
            writer.WriteLine("}");
        }

        private void WriteFunction(MethodInfo methodInfo, string objectType)
        {
            ParameterInfo[] info = methodInfo.GetParameters();
            string type = methodInfo.IsConstructor ? "new" : (methodInfo.ReturnType == typeof(void) ? "set" : "get");
            string functionName = $"____{type}{methodInfo.Name}StaticBindings____";

            writer.WriteLine($"public void {functionName}(InterpreterRuntime runtime) {{");

            if (methodInfo.IsConstructor == false)
                writer.Write($"if (!runtime.Pop(out {methodInfo.DeclaringType.FullName} result)");
            else
                writer.Write($"if (");

            for (int i = 0; i < info.Length; i++)
            {
                writer.Write($" || !runtime.Pop(out {info[i].ParameterType.FullName} a{i})");
            }

            if (methodInfo.IsConstructor)
            {
                if (info.Length > 0)
                    writer.Write($"!runtime.Push(new {methodInfo.Name}({string.Join(",", Enumerable.Range(0, info.Length).Select(x => "a" + x))}), true)");
                else
                    writer.Write($"!runtime.Push(new {methodInfo.Name}(), true)");
            }
            else if (methodInfo.ReturnType != typeof(void))
            {
                if (info.Length > 0)
                    writer.Write($" || !runtime.Push(result.{methodInfo.Name}({string.Join(",", Enumerable.Range(0, info.Length).Select(x => "a" + x))}), true)");
                else
                    writer.Write($" || !runtime.Push(result.{methodInfo.Name}(), true)");
            }
            writer.WriteLine(")");

            writer.WriteLine("Log.Error(\"Function failed\");");

            if (methodInfo.IsConstructor == false && methodInfo.ReturnType == typeof(void))
            {
                writer.WriteLine("else");
                if (info.Length > 0)
                    writer.Write($"result.{methodInfo.Name}({string.Join(",", Enumerable.Range(0, info.Length).Select(x => "a" + x))});");
                else
                    writer.Write($"result.{methodInfo.Name}();");
            }
            writer.WriteLine("}");

            string name = $"{methodInfo.GetCustomAttribute<DOMLCustomiseAttribute>()?.Name ?? methodInfo.Name}";

            switch (type)
            {
            case "new":
                registerCalls.Add($"InstructionRegister.RegisterConstructor(\"{name}\", {functionName});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterConstructor(\"{name}\");");
                break;
            case "get":
                registerCalls.Add($"InstructionRegister.RegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, {functionName});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
                break;
            case "set":
                registerCalls.Add($"InstructionRegister.RegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\", {info.Length}, {functionName});");
                unregisterCalls.Add($"InstructionRegister.UnRegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
                break;
            default:
                throw new NotImplementedException("Type not implemented");
            }
        }

        private void WriteProperty(PropertyInfo propertyInfo, string objectType)
        {
            string name = $"{propertyInfo.GetCustomAttribute<DOMLCustomiseAttribute>()?.Name ?? propertyInfo.Name}";

            if (propertyInfo.CanRead && (propertyInfo.GetMethod?.IsPublic ?? false))
            {
                // Write Getter
                writer.WriteLine($"public void ____Get{propertyInfo.Name}StaticBindings____(InterpreterRuntime runtime) {{");
                writer.WriteLine($"if (!runtime.Pop(out {propertyInfo.DeclaringType.Name} result) || !runtime.Push(result.{propertyInfo.Name}, true))");
                writer.WriteLine("Log.Error(\"Getter failed\");");
                writer.WriteLine("}");

                registerCalls.Add($"InstructionRegister.RegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, ____Get{propertyInfo.Name}StaticBindings____);");
                unregisterCalls.Add($"InstructionRegister.UnRegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
            }

            if (propertyInfo.CanWrite && (propertyInfo.SetMethod?.IsPublic ?? false))
            {
                // Write Setter
                writer.WriteLine($"public void ____Set{propertyInfo.Name}StaticBindings____(InterpreterRuntime runtime) {{");
                writer.WriteLine($"if (runtime.Pop(out {propertyInfo.DeclaringType.FullName} result) || !runtime.Pop(out result.{propertyInfo.Name}))");
                writer.WriteLine("Log.Error(\"Setter failed\");");
                writer.WriteLine("}");

                registerCalls.Add($"InstructionRegister.RegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, ____Set{propertyInfo.Name}StaticBindings____);");
                unregisterCalls.Add($"InstructionRegister.UnRegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
            }
        }

        private void WriteField(FieldInfo fieldInfo, string objectType)
        {
            string name = $"{fieldInfo.GetCustomAttribute<DOMLCustomiseAttribute>()?.Name ?? fieldInfo.Name}";

            // Write Getter
            writer.WriteLine($"public void ____Get{fieldInfo.Name}StaticBindings____(InterpreterRuntime runtime) {{");
            writer.WriteLine($"if (!runtime.Pop(out {fieldInfo.DeclaringType.FullName} result) || !runtime.Push(result.{fieldInfo.Name}, true))");
            writer.WriteLine("Log.Error(\"Getter failed\");");
            writer.WriteLine("}");

            registerCalls.Add($"InstructionRegister.RegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, ____Get{fieldInfo.Name}StaticBindings____);");
            unregisterCalls.Add($"InstructionRegister.UnRegisterGetter(\"{name}\", \"{rootNamespace}.{objectType}\");");

            if (fieldInfo.IsLiteral == false && fieldInfo.IsInitOnly == false)
            {
                // Write Setter
                writer.WriteLine($"public void ____Set{fieldInfo.Name}StaticBindings____(InterpreterRuntime runtime) {{");
                writer.WriteLine($"if (runtime.Pop(out {fieldInfo.DeclaringType.FullName} result) || !runtime.Pop(out result.{fieldInfo.Name}))");
                writer.WriteLine("Log.Error(\"Setter failed\");");
                writer.WriteLine("}");

                registerCalls.Add($"InstructionRegister.RegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\", {1}, ____Set{fieldInfo.Name}StaticBindings____);");
                unregisterCalls.Add($"InstructionRegister.UnRegisterSetter(\"{name}\", \"{rootNamespace}.{objectType}\");");
            }
        }

        private void Finish()
        {
            writer.Dispose();
        }
    }
}
