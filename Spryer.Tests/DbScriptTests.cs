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
}
