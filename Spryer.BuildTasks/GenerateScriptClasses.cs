namespace Spryer.BuildTasks;

using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Scripting;

public class GenerateScriptClasses : Task
{
    [Required]
    public ITaskItem[] ScriptFiles { get; set; } = [];

    [Output]
    public ITaskItem[] GeneratedFiles { get; private set; } = [];

    public override bool Execute()
    {
        var generatedFiles = new List<ITaskItem>();

        foreach (var scriptFileItem in this.ScriptFiles)
        {
            var scriptFilePath = scriptFileItem.ItemSpec;
            var scriptMapLoader = new DbScriptMap.Loader
            {
                FileName = scriptFilePath,
                CollectsPragmas = true,
            };

            if (scriptMapLoader.TryLoadScripts())
            {
                var scriptMap = scriptMapLoader.GetScriptMap();
                var scriptClass = new ScriptClass(scriptMap)
                {
                    Namespace = scriptFileItem.GetMetadata("CustomToolNamespace"),
                };
                var code = new CodeBuilder();
                scriptClass.Generate(code);

                var generatedFilePath = scriptFilePath;
                if (scriptFileItem.GetMetadata("LastGenOutput") is string lastGenOutput)
                {
                    generatedFilePath = Path.Combine(Path.GetDirectoryName(scriptFilePath), lastGenOutput);
                }
                else
                {
                    generatedFilePath = Path.ChangeExtension(scriptFilePath, ".cs");
                    scriptFileItem.SetMetadata("LastGenOutput",
                        NPath.GetRelativePath(Path.GetDirectoryName(scriptFilePath), generatedFilePath));
                }

                File.WriteAllText(generatedFilePath, code.ToString());
                generatedFiles.Add(new TaskItem(generatedFilePath));
            }
        }

        this.GeneratedFiles = generatedFiles.ToArray();

        return !this.Log.HasLoggedErrors;
    }
}
