namespace Spryer.BuildTasks;

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Scripting;

/// <summary>
/// Generates C# classes from DbScriptMap files.
/// </summary>
public class GenerateDbScriptClasses : Task
{
    /// <summary>
    /// Gets or sets the script files to generate classes that load the scripts at runtime.
    /// </summary>
    [Required]
    public ITaskItem[] ScriptFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the script files to generate classes that use inlined scripts.
    /// </summary>
    [Required]
    public ITaskItem[] InlineScriptFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the source files that might depend on the scripts.
    /// </summary>
    public ITaskItem[] SourceFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the root directory for the generated files.
    /// </summary>
    public string? RootDirectory { get; set; }

    /// <summary>
    /// Gets or sets the root namespace for the generated classes.
    /// </summary>
    public string? RootNamespace { get; set; }

    /// <summary>
    /// Gets the generated files.
    /// </summary>
    [Output]
    public ITaskItem[] GeneratedFiles { get; private set; } = [];

    /// <summary>
    /// Gets the script files with LastGenOutput metadata.
    /// </summary>
    [Output]
    public ITaskItem[] ScriptFilesWithLastGenOutput { get; private set; } = [];

    /// <summary>
    /// Gets the source files with LastGenOutput metadata.
    /// </summary>
    [Output]
    public ITaskItem[] InlineScriptFilesWithLastGenOutput { get; private set; } = [];

    /// <inheritdoc/>
    public override bool Execute()
    {
        var generatedFiles = new List<ITaskItem>();

        this.ScriptFilesWithLastGenOutput = GenerateCode(this.ScriptFiles, generatedFiles, inlineScripts: false);
        //this.InlineScriptFilesWithLastGenOutput = GenerateCode(this.InlineScriptFiles, generatedFiles, inlineScripts: true);

        this.GeneratedFiles = generatedFiles.ToArray();

        return !this.Log.HasLoggedErrors;
    }

    private ITaskItem[] GenerateCode(ITaskItem[] scriptFileItems, List<ITaskItem> generatedFiles, bool inlineScripts)
    {
        var itemsWithLastGenOutput = new List<ITaskItem>();

        foreach (var scriptFileItem in scriptFileItems)
        {
            try
            {
                var scriptFilePath = scriptFileItem.GetMetadata("FullPath");

                // load scripts
                var scriptMapLoader = new DbScriptMap.Loader
                {
                    FileName = scriptFilePath,
                    CollectsPragmas = true,
                };
                if (!scriptMapLoader.TryLoadScripts()) continue;

                // generate code
                var scriptMap = scriptMapLoader.GetScriptMap();
                var scriptClass = new ScriptClass(scriptMap)
                {
                    Namespace = scriptFileItem.GetMetadata("CustomToolNamespace") ?? this.RootNamespace,
                };
                var code = new CodeBuilder();
                scriptClass.Generate(code);

                // save file
                var generatedFileId = GetGeneratedFileId(scriptFileItem);

                var generatedFilePath = Path.Combine(
                    this.RootDirectory ?? scriptFileItem.GetMetadata("DefiningProjectDirectory") ?? Path.GetDirectoryName(scriptFilePath) ?? string.Empty,
                    generatedFileId);

                this.Log.LogMessage(MessageImportance.Normal, $"Generating {generatedFilePath}");
                var directory = Path.GetDirectoryName(generatedFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(generatedFilePath, code.ToString());

                // update metadata
                var generatedFileItem = new TaskItem(generatedFilePath);
                generatedFileItem.SetMetadata("DependentUpon", scriptFileItem.GetMetadata("Identity"));
                generatedFiles.Add(generatedFileItem);

                if (scriptFileItem.GetMetadata("LastGenOutput") is not string lastGenOutput || string.IsNullOrWhiteSpace(lastGenOutput))
                {
                    scriptFileItem.SetMetadata("LastGenOutput", generatedFileId);
                    itemsWithLastGenOutput.Add(scriptFileItem);
                }
            }
            catch (Exception x)
            {
                this.Log.LogErrorFromException(x, true);
            }
        }

        return itemsWithLastGenOutput.ToArray();
    }

    private string GetGeneratedFileId(ITaskItem scriptFileItem)
    {
        if (scriptFileItem.GetMetadata("LastGenOutput") is string lastGenOutput && !string.IsNullOrWhiteSpace(lastGenOutput))
            return lastGenOutput;

        var scriptFileId = scriptFileItem.GetMetadata("Identity");
        var sourceFileId = Array.Find(this.SourceFiles,
            it => string.Equals(it.GetMetadata("DependentUpon"), scriptFileId, StringComparison.Ordinal))?.GetMetadata("Identity");
        if (!string.IsNullOrWhiteSpace(sourceFileId))
            return sourceFileId;

        return scriptFileId + ".cs";
    }
}
