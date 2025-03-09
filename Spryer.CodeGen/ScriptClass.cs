﻿namespace Spryer.CodeGen;

using System;
using System.Linq;
using System.Reflection;
using System.Text;

sealed class ScriptClass : ICodeGenerator
{
    private const string DefaultUsings =
        """
        System;
        System.Collections.Generic;
        System.Data;
        System.Threading.Tasks;
        Dapper;
        Spryer;
        """;

    private readonly DbScriptMap scriptMap;

    public ScriptClass(DbScriptMap scriptMap)
    {
        this.scriptMap = scriptMap;
    }

    public string? Name { get; set; }
    public string? Namespace { get; set; }
    public string Usings { get; set; } = DefaultUsings;

    public void Generate(CodeBuilder code)
    {
        GenerateHeader(code);
        code.AppendLine();
        GenerateUsings(code);
        code.AppendLine();
        GenerateClass(code);
    }

    private void GenerateClass(CodeBuilder code)
    {
        code.AppendLine($"internal static partial class {GetClassName()}");
        code.AppendLine("{");

        using (code.Indent())
        {
            GenerateCtor(code);

            foreach (var script in this.scriptMap.Enumerate())
            {
                code.AppendLine();
                var scriptMethod = new ScriptMethod(script);
                scriptMethod.Generate(code);
            }
        }

        code.AppendLine("}");
    }

    public string GetClassName()
    {
        if (string.IsNullOrWhiteSpace(this.Name))
        {
            return Path.GetFileName(this.scriptMap.Source).ToPascalCase() + "Extensions";
        }

        return this.Name.ToPascalCase();
    }

    private void GenerateCtor(CodeBuilder code)
    {
        code.AppendLines(
            $$"""
            private static readonly DbScriptMap sql;

            static {{GetClassName()}}()
            {
                sql = DbScriptMap.Load("{{Path.GetFileName(this.scriptMap.Source)}}");
            }
            """);
    }

    private void GenerateUsings(CodeBuilder code)
    {
        var usings = DefaultUsings.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Concat(this.Usings.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Concat(this.scriptMap.Pragmas["using"])
            //.Order(StringComparer.Ordinal)
            .Distinct(StringComparer.Ordinal);

        foreach (var @using in usings)
        {
            code.AppendLine($"using {@using};");
        }
    }

    private void GenerateHeader(CodeBuilder code)
    {
        code.AppendLines(
            $"""
            /*
             * This file was generated by Spryer.CodeGen {GetVersion()}
             * https://github.com/vborovikov/spryer
             *
             * Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
             * Source: {Path.GetFileName(this.scriptMap.Source)} {this.scriptMap.Version}
             * Scripts: {this.scriptMap.Count}
             */

            namespace {GetNamespace()};
            """);
    }

    private string GetNamespace()
    {
        var ns = this.Namespace;

        if (string.IsNullOrWhiteSpace(ns))
        {
            var nsBuilder = new StringBuilder();

            for (var dir = Directory.GetParent(this.scriptMap.Source); dir is not null; dir = dir.Parent)
            {
                var csproj = dir.EnumerateFiles("*.csproj").FirstOrDefault();
                if (csproj is not null)
                {
                    var projectName = Path.GetFileNameWithoutExtension(csproj.Name);
                    if (nsBuilder.Length > 0) nsBuilder.Insert(0, '.');
                    nsBuilder.Insert(0, projectName);
                    break;
                }

                if (nsBuilder.Length > 0) nsBuilder.Insert(0, '.');
                nsBuilder.Insert(0, dir.Name);
            }
            if (nsBuilder.Length == 0)
            {
                nsBuilder.Append("Spryer.Generated");
            }

            ns = nsBuilder.ToString();
        }

        return ns;
    }

    private static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return version ?? string.Empty;
    }
}
