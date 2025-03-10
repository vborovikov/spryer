namespace Spryer.Tests;

extern alias Standalone;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Standalone.Spryer.Scripting;

[TestClass]
public class StandaloneDbScriptTests
{
    [TestMethod]
    public void Load_DefaultResource_FoundScripts()
    {
        var sql = DbScriptMap.Load();

        Assert.AreEqual(
            """
            SELECT Id, Name, Email, Address 
            FROM Users
            WHERE Id = @Id;
            """,
            sql["TestSelect"]);
    }

    [TestMethod]
    public void Load_DefaultResource_FoundVersion()
    {
        var sql = DbScriptMap.Load();
        Assert.AreEqual(new Version(1, 0, 1), sql.Version);
    }

    [TestMethod]
    public void Load_MultilinePragma_Parsed()
    {
        var sql = DbScriptMap.Load();
        var script = sql.Find("Multiline");

        Assert.IsNotNull(script);
        Assert.AreEqual(DbScriptType.ExecuteScalar, script.Type);
        Assert.AreEqual("select 1;", script.Text);
        Assert.AreEqual(10, script.Parameters.Length);
    }

    [TestMethod]
    public void Load_Wildcard_Loaded()
    {
        var sql = DbScriptMap.Load("Test?.sql");
        Assert.AreEqual(2, sql.Count);
    }

    [DataTestMethod]
    [DataRow("*.*", true)]
    [DataRow("*.sql", true)]
    [DataRow("script.sql", false)]
    [DataRow("", false)]
    [DataRow("scrip?.sql", true)]
    [DataRow("script.[cs]ql", false)]
    public void HasWildcards_Patterns_Detected(string pattern, bool flag)
    {
        Assert.AreEqual(flag, pattern.HasWildcard());
    }

    [TestMethod]
    public void Load_PragmaInComment_ShouldntExist()
    {
        var sql = DbScriptMap.Load("Wrong.sql");
        Assert.AreEqual("", sql["ShouldntExist"]);
    }

    [TestMethod]
    public void Load_MultipleVersions_LatestVersion()
    {
        var sql = DbScriptMap.Load("Wrong.sql");
        Assert.AreEqual(new Version(0, 0, 2), sql.Version);
    }

    [TestMethod]
    public void Load_ScriptNameWithSpacesInQuotes_Parsed()
    {
        var sql = DbScriptMap.Load("Wrong.sql");
        Assert.AreNotEqual("", sql["Name Space"]);
    }

    [TestMethod]
    public void Load_SpaceBeforePragmaName_Parsed()
    {
        var sql = DbScriptMap.Load("Wrong.sql");
        Assert.AreNotEqual("", sql["PragmaSpacesSimpleMeta"]);
    }

    [TestMethod]
    public void Load_SpaceBeforePragmaFullMeta_Parsed()
    {
        var sql = DbScriptMap.Load("Wrong.sql");
        var script = sql.Find("PragmaSpacesFullMeta");
        Assert.IsNotNull(script);
        Assert.AreNotEqual("", script.Text);
        Assert.AreEqual(2, script.Parameters.Length);
    }

    [TestMethod]
    public void Load_VersionMismatch_ExceptionThrown()
    {
        Assert.ThrowsException<ScriptMapVersionMismatchException>(
            () => DbScriptMap.Load("Wrong.sql", expectedVersion: new(3, 5)));
    }

    [DataTestMethod]
    [DataRow("--@script \"MyScript\"\rSELECT 1;", DbScriptType.Query)]
    [DataRow("--@execute \"MyExecute\"\rEXEC MyProc;", DbScriptType.Execute)]
    [DataRow("--@execute-reader \"MyReader\"\rSELECT * FROM MyTable;", DbScriptType.ExecuteReader)]
    [DataRow("--@execute-scalar \"MyScalar\"\rSELECT COUNT(*) FROM MyTable;", DbScriptType.ExecuteScalar)]
    [DataRow("--@query \"MyQuery\"\rSELECT * FROM MyTable;", DbScriptType.Query)]
    [DataRow("--@query-first \"MyFirst\"\rSELECT * FROM MyTable;", DbScriptType.QueryFirst)]
    [DataRow("--@query-first-default \"MyFirstDefault\"\rSELECT * FROM MyTable;", DbScriptType.QueryFirstOrDefault)]
    [DataRow("--@query-single \"MySingle\"\rSELECT * FROM MyTable;", DbScriptType.QuerySingle)]
    [DataRow("--@query-single-default \"MySingleDefault\"\rSELECT * FROM MyTable;", DbScriptType.QuerySingleOrDefault)]
    [DataRow("--@query-multiple \"MyMultiple\"\rSELECT * FROM MyTable;", DbScriptType.QueryMultiple)]
    [DataRow("--@query-unbuffered \"MyUnbuffered\"\rSELECT * FROM MyTable;", DbScriptType.QueryUnbuffered)]
    public void DbScript_TryParse_ValidPragma_SetsTypeCorrectly(string pragmaText, DbScriptType expectedType)
    {
        var pragma = CreatePragma(pragmaText);
        var result = DbScript.TryParse(pragma, out var script);

        Assert.IsTrue(result);
        Assert.IsNotNull(script);
        Assert.AreEqual(expectedType, script.Type);
    }

    [TestMethod]
    public void DbScript_TryParse_InvalidPragma_ReturnsFalse()
    {
        var pragma = CreatePragma("--@invalid \"InvalidScript\"\nInvalid SQL;");
        var result = DbScript.TryParse(pragma, out var script);

        Assert.IsFalse(result);
        Assert.IsNull(script);
    }

    private static Pragma CreatePragma(string pragmaText)
    {
        var pragmaEnumerator = Pragma.Enumerate(pragmaText);
        Assert.IsTrue(pragmaEnumerator.MoveNext());
        return pragmaEnumerator.Current;
    }
}
