﻿// Copyright Dirk Lemstra https://github.com/dlemstra/Magick.NET.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace FileGenerator.Native
{
    internal sealed class NativeInstanceGenerator : NativeCodeGenerator
    {
        public NativeInstanceGenerator(NativeClassGenerator parent)
            : base(parent)
        {
        }

        private bool IsNativeStatic
        {
            get
            {
                if (Class.HasInstance)
                    return false;

                if (Class.Methods.Any(method => method.CreatesInstance))
                    return false;

                if (Class.Methods.Any(method => method.Throws))
                    return false;

                if (Class.Methods.Any(method => !method.IsStatic))
                    return false;

                return true;
            }
        }

        public void Write()
        {
            if (Class.IsStatic)
            {
                WriteLine("private unsafe static class Native" + Class.ClassName);
                WriteStartColon();

                WriteStaticConstructor();
            }
            else
            {
                if (!IsDynamic(Class.Name))
                    WriteLine("private Native" + Class.ClassName + " _nativeInstance;");

                string baseClass;
                if (IsNativeStatic)
                    baseClass = string.Empty;
                else if (!Class.HasInstance)
                    baseClass = " : NativeHelper";
                else if (Class.IsConst)
                    baseClass = " : ConstNativeInstance";
                else
                    baseClass = " : NativeInstance";

                WriteLine("private unsafe " + (IsNativeStatic ? "static" : "sealed") + " class Native" + Class.ClassName + baseClass);
                WriteStartColon();

                WriteStaticConstructor();

                WriteDispose();

                WriteConstructors();

                WriteTypeName();
            }

            WriteProperties();

            WriteMethods();

            WriteEndColon();

            if (!Class.HasInstance || Class.IsConst || Class.IsStatic)
                return;

            if (IsDynamic(Class.Name))
                WriteCreateInstance();
        }

        private string? CreateCleanupString(MagickMethod method)
        {
            if (method.Cleanup == null)
            {
                if (!Class.HasInstance && method.ReturnType.IsInstance)
                    return "Dispose(result);";

                return null;
            }

            var cleanup = method.Cleanup;

            string result = cleanup.Name + "(result";
            if (cleanup.Arguments.Count() > 0)
                result += ", " + string.Join(", ", cleanup.Arguments);
            return result + ");";
        }

        private string GetAction(string action, MagickType type)
        {
            if (type.IsString)
                return "UTF8Marshaler.NativeToManaged(" + action + ");";
            else
                return type.ManagedTypeCast + action + ";";
        }

        private void WriteCleanup(string cleanupString)
        {
            WriteLine("var magickException = MagickExceptionHelper.Create(exception);");
            WriteIf("magickException == null", "return result;");
            WriteLine("if (magickException is MagickErrorException)");
            WriteStartColon();
            WriteIf("result != IntPtr.Zero", cleanupString);
            WriteLine("throw magickException;");
            WriteEndColon();
            if (!Class.IsStatic)
                WriteLine("RaiseWarning(magickException);");
        }

        private void WriteConstructors()
        {
            if (!Class.HasInstance)
                return;

            if (!Class.IsConst && !Class.HasNoConstructor)
            {
                var arguments = GetArgumentsDeclaration(Class.Constructor.Arguments);
                WriteLine("public Native" + Class.Name + "(" + arguments + ")");
                WriteStartColon();

                WriteHelpingVariableStart(Class.Constructor.Arguments);

                WriteThrowStart(Class.Constructor.Throws);

                arguments = GetNativeArgumentsCall(Class.Constructor.Arguments);
                WriteNativeIfContent("Instance = NativeMethods.{0}." + Class.Name + "_Create(" + arguments + ");");

                if (Class.Constructor.Throws)
                    WriteLine("CheckException(exception, Instance);");

                WriteIf("Instance == IntPtr.Zero", "throw new InvalidOperationException();");

                WriteHelpingVariableEnd(Class.Constructor.Arguments);

                WriteEndColon();
            }

            if (!Class.HasNativeConstructor)
                return;

            WriteLine("public Native" + Class.Name + "(IntPtr instance)");
            WriteStartColon();
            WriteLine("Instance = instance;");
            WriteEndColon();
        }

        private void WriteHelpingVariableEnd(IEnumerable<MagickArgument> arguments)
        {
            foreach (var argument in arguments)
            {
                if (NeedsCreate(argument.Type) || argument.Type.IsFixed)
                    WriteEndColon();
            }
        }

        private void WriteHelpingVariableEnd(MagickProperty property)
        {
            if (NeedsCreate(property.Type) || property.Type.IsFixed)
                WriteEndColon();
        }

        private void WriteCreateInstance()
        {
            var name = Class.Name;
            if (Class.IsQuantumType)
                name = "I" + name + "<QuantumType>";
            else if (Class.HasInterface)
                name = "I" + name;

            if (Class.DynamicMode.HasFlag(DynamicMode.ManagedToNative))
            {
                WriteLine("internal static INativeInstance CreateInstance(" + name + "? instance)");
                WriteStartColon();
                WriteIf("instance == null", "return NativeInstance.Zero;");
                if (Class.IsQuantumType || Class.HasInterface)
                    WriteLine("return " + Class.Name + ".CreateNativeInstance(instance);");
                else
                    WriteLine("return instance.CreateNativeInstance();");
                WriteEndColon();
            }

            if (Class.DynamicMode.HasFlag(DynamicMode.NativeToManaged))
            {
                WriteLine("internal static " + name + "? CreateInstance(IntPtr instance)");
                WriteStartColon();
                WriteIf("instance == IntPtr.Zero", "return null;");
                WriteLine("using (Native" + Class.Name + " nativeInstance = new Native" + Class.Name + "(instance))");
                WriteStartColon();
                WriteLine("return new " + Class.Name + "(nativeInstance);");
                WriteEndColon();
                WriteEndColon();
            }
        }

        private void WriteCreateOut(IEnumerable<MagickArgument> arguments)
        {
            foreach (var argument in arguments)
            {
                if (!argument.IsOut || !NeedsCreate(argument.Type))
                    continue;

                WriteLine(argument.Name + " = " + argument.Type.ManagedName + ".CreateInstance(" + argument.Name + "Native);");
            }
        }

        private void WriteHelpingVariableStart(IEnumerable<MagickArgument> arguments)
        {
            foreach (var argument in arguments)
            {
                if (!NeedsCreate(argument.Type) && !argument.Type.IsFixed)
                    continue;

                if (argument.IsOut)
                    WriteCreateStartOut(argument.Name, argument.Type);
                else
                    WriteHelpingVariableStart(argument.Name, argument.Type);
            }
        }

        private void WriteHelpingVariableStart(MagickProperty property)
        {
            if (NeedsCreate(property.Type) || property.Type.IsFixed)
                WriteHelpingVariableStart("value", property.Type);
        }

        private void WriteHelpingVariableStart(string name, MagickType type)
        {
            if (type.IsFixed)
            {
                WriteLine("fixed (" + type.FixedName + " " + name + "Fixed = " + name + ")");
            }
            else
            {
                Write("using (var " + name + "Native = ");

                if (type.IsString)
                    Write("UTF8Marshaler");
                else
                    Write(type.ManagedName);

                WriteLine(".CreateInstance(" + name + "))");
            }

            WriteStartColon();
        }

        private void WriteCreateStartOut(string name, MagickType type)
        {
            WriteLine("using (INativeInstance " + name + "Native = " + type.ManagedName + ".CreateInstance())");
            WriteStartColon();
            WriteLine("IntPtr " + name + "NativeOut = " + name + "Native.Instance;");
        }

        private void WriteDispose()
        {
            if (Class.IsConst || !Class.HasInstance)
                return;

            WriteLine("protected override void Dispose(IntPtr instance)");
            WriteStartColon();

            if (Class.HasNoConstructor)
            {
                WriteLine("DisposeInstance(instance);");
                WriteEndColon();

                WriteLine("public static void DisposeInstance(IntPtr instance)");
                WriteStartColon();
            }

            WriteNativeIfContent("NativeMethods.{0}." + Class.Name + "_Dispose(instance);");
            WriteEndColon();
        }

        private void WriteTypeName()
        {
            if (!Class.HasInstance)
                return;

            WriteLine("protected override string TypeName");
            WriteStartColon();
            WriteLine("get");
            WriteStartColon();
            WriteLine("return nameof(" + Class.Name + ");");
            WriteEndColon();
            WriteEndColon();
        }

        private void WriteMethods()
        {
            foreach (var method in Class.Methods)
            {
                if (HasSpan(method))
                    WriteLine("#if NETSTANDARD2_1");

                var arguments = GetArgumentsDeclaration(method.Arguments);
                var isStatic = Class.IsStatic || ((method.IsStatic && !method.Throws) && !method.CreatesInstance);
                var typeName = GetTypeName(method.ReturnType);
                WriteLine("public " + (isStatic ? "static " : string.Empty) + typeName + (IsNullable(method.ReturnType) ? "?" : string.Empty) + " " + method.Name + "(" + arguments + ")");

                WriteStartColon();

                WriteHelpingVariableStart(method.Arguments);

                WriteThrowStart(method.Throws);

                var hasResult = method.CreatesInstance || IsDynamic(method.ReturnType) || !method.ReturnType.IsVoid;

                if (hasResult)
                    WriteLine(method.ReturnType.NativeName + " result;");

                arguments = GetNativeArgumentsCall(method);
                var action = "NativeMethods.{0}." + Class.Name + "_" + method.Name + "(" + arguments + ");";
                if (hasResult)
                    action = "result = " + action;

                WriteNativeIfContent(action);

                if (method.Throws)
                {
                    WriteCreateOut(method.Arguments);

                    var cleanupString = CreateCleanupString(method);
                    if (!string.IsNullOrEmpty(cleanupString))
                        WriteCleanup(cleanupString);
                    else if (method.CreatesInstance && !Class.IsConst)
                        WriteLine("CheckException(exception, result);");
                    else
                        WriteCheckException(true);
                }

                if (hasResult && method.ReturnType.IsVoid)
                {
                    WriteLine("if (result != IntPtr.Zero)");
                    WriteLine("  Instance = result;");
                }
                else
                {
                    WriteReturn(method.ReturnType);
                }

                WriteHelpingVariableEnd(method.Arguments);

                WriteEndColon();

                if (HasSpan(method))
                    WriteLine("#endif");
            }
        }

        private void WriteProperties()
        {
            foreach (var property in Class.Properties)
            {
                Write("public ");
                if (Class.IsStatic)
                    Write("static ");

                var typeName = GetTypeName(property.Type);

                WriteLine(typeName + (IsNullable(property.Type) ? "?" : string.Empty) + " " + property.Name);
                WriteStartColon();

                WriteLine("get");
                WriteStartColon();

                WriteThrowStart(property.Throws);

                WriteLine(property.Type.NativeName + " result;");
                string arguments = !Class.IsStatic ? "Instance" : string.Empty;
                if (property.Throws)
                    arguments += ", out exception";
                WriteNativeIfContent("result = NativeMethods.{0}." + Class.Name + "_" + property.Name + "_Get(" + arguments + ");");
                WriteCheckException(property.Throws);
                WriteReturn(property.Type);

                WriteEndColon();

                if (!property.IsReadOnly)
                {
                    WriteLine("set");
                    WriteStartColon();

                    WriteHelpingVariableStart(property);

                    string value = property.Type.NativeTypeCast + "value";
                    if (NeedsCreate(property.Type))
                        value = "valueNative.Instance";

                    arguments = (Class.IsStatic ? string.Empty : "Instance, ") + value;
                    if (property.Throws)
                        arguments += ", out exception";

                    WriteThrowStart(property.Throws);
                    WriteNativeIfContent("NativeMethods.{0}." + Class.Name + "_" + property.Name + "_Set(" + arguments + ");");
                    WriteCheckException(property.Throws);

                    WriteHelpingVariableEnd(property);

                    WriteEndColon();
                }

                WriteEndColon();
            }
        }

        private void WriteReturn(MagickType type)
        {
            if (type.IsVoid)
                return;

            if (IsDynamic(type))
                WriteLine("return " + type.ManagedName + ".CreateInstance(result);");
            else if (type.IsNativeString)
                WriteLine("return UTF8Marshaler.NativeToManagedAndRelinquish(result);");
            else if (type.IsString)
                WriteLine("return UTF8Marshaler.NativeToManaged(result);");
            else if (type.HasInstance)
                WriteLine("return result.Create" + type.ManagedName + "();");
            else
                WriteLine("return " + type.ManagedTypeCast + "result;");
        }

        private void WriteStaticConstructor()
        {
            if (Class.Name == "Environment")
                return;

            WriteLine("static Native" + Class.ClassName + "() { Environment.Initialize(); }");
        }

        private string GetTypeName(MagickType type)
        {
            var typeName = type.ManagedName;

            if (IsQuantumType(type))
                return "I" + typeName + "<QuantumType>";

            if (HasInterface(type))
                typeName = "I" + typeName;

            return typeName;
        }
    }
}
