namespace Spryer.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DbScriptTests
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
        Assert.AreNotEqual("", sql["PragmaSpacesFullMeta"]);
    }
}
