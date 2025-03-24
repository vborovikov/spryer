# Spryer.BuildTasks

The project provides MSBuild tasks to automate the generation of C# classes from SQL script files managed by the Spryer library's `DbScriptMap` class. This simplifies the process of working with SQL scripts within a .NET project.

## Usage

After installing the NuGet package you need to add several elements to your `.csproj` file to configure the build task. This involves specifying the SQL script files and the desired output location and namespace for the generated C# classes. Here's an example:

```xml
<ItemGroup>
    <!-- SQL script files (can be embedded resources or files on the file system loaded at runtime) -->
    <EmbeddedResource Include="Scripts\MyScripts.sql">
        <Generator>DbScriptMapCodeGenerator</Generator>  <!-- This is crucial for the task to find the scripts -->
        <CustomToolNamespace>MyProject.Scripts</CustomToolNamespace> <!-- A namespace for the generated classes -->
    </EmbeddedResource>

    <!-- Example of an inline script file (scripts are baked into the generated classes) -->
    <None Include="Scripts\InlineScripts.sql">
        <Generator>DbScriptMapCodeGenerator</Generator>
        <CustomToolNamespace>MyProject.Scripts</CustomToolNamespace>
    </None>
</ItemGroup>
```

After the build completes, you can use the generated C# classes in your application code to access and execute the SQL scripts. Each class will contain extensions methods that correspond to the scripts defined in the SQL files. For example:

```sql
-- MyScripts.sql

--@query-first-default GetResult(@Id uniqueidentifier)
select * from Results where Id = @Id;
```

```csharp
// Application code
using MyProject.Scripts;

// Accessing a script from the generated class
var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
var result = await connection.GetResultAsync<ResultObject>(new { Id = resultId });
```