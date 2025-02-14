namespace Spryer.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DbScriptTests
{
    [TestMethod]
    public void Load_DefaultResource_FoundParsed()
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
}
