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
    [DataRow("literal", "fliteral")]
    [DataRow("literal", "foo/literal")]
    [DataRow("literal", "literals")]
    [DataRow("literal", "literals/foo")]
    [DataRow("path/hats*nd", "path/hatsblahn")]
    [DataRow("path/hats*nd", "path/hatsblahndt")]
    [DataRow("path/?atstand", "path/moatstand")]
    [DataRow("path/?atstand", "path/batstands")]
    [DataRow("/**/file.csv", "/file.txt")]
    [DataRow("/*file.txt", "/folder")]
    [DataRow("Shock* 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
    [DataRow("*ave 12", "wave 12/")]
    [DataRow("C:\\THIS_IS_A_DIR\\**\\somefile.txt", "C:\\THIS_IS_A_DIR\\awesomefile.txt")]
    [DataRow("C:\\name\\**", "C:\\name.ext")]
    [DataRow("C:\\name\\**", "C:\\name_longer.ext")]
    [DataRow("Bumpy/**/AssemblyInfo.cs", "Bumpy.Test/Properties/AssemblyInfo.cs")]
    [DataRow("C:\\sources\\x-y 1\\BIN\\DEBUG\\COMPILE\\**\\MSVC*120.DLL", "C:\\sources\\x-y 1\\BIN\\DEBUG\\COMPILE\\ANTLR3.RUNTIME.DLL")]
    [DataRow("[list]s", "LS")]
    [DataRow("[list]s", "iS")]
    [DataRow("[list]s", "Is")]
    [DataRow("range/[a-b][C-D]", "range/ac")]
    [DataRow("range/[a-b][C-D]", "range/Ad")]
    [DataRow("range/[a-b][C-D]", "range/BD")]
    [DataRow(@"abc/**", @"abcd")]
    [DataRow(@"**\segment1\**\segment2\**", @"C:\test\segment1\src\segment2")]
    [DataRow(@"**/.*", "foobar.")]
    [DataRow(@"**/~*", "/")]
    public void Matches_NoneMatches_ReturnsFalse(string pattern, string test)
    {
        var result = pattern.Matches(test);
        Assert.IsFalse(result);
    }

    [DataTestMethod]
    [DataRow("literal", "literal")]
    [DataRow("a/literal", "a/literal")]
    [DataRow("path/*atstand", "path/fooatstand")]
    [DataRow("path/hats*nd", "path/hatsforstand")]
    [DataRow("path/?atstand", "path/hatstand")]
    [DataRow("path/?atstand?", "path/hatstands")]
    [DataRow("/**/file.*", "/folder/file.csv")]
    [DataRow("**/file.*", "/file.txt")]
    [DataRow("/*file.txt", "/file.txt")]
    [DataRow("C:\\THIS_IS_A_DIR\\*", "C:\\THIS_IS_A_DIR\\somefile")]
    [DataRow("/DIR1/*/*", "/DIR1/DIR2/file.txt")]
    [DataRow("~/*~3", "~/abc123~3")]
    [DataRow("**", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
    [DataRow("**", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12.txt")]
    [DataRow("Stuff, *", "Stuff, x")]
    [DataRow("\"Stuff*", "\"Stuff")]
    [DataRow("path/**/somefile.txt", "path//somefile.txt")]
    [DataRow("**/app*.js", "dist/app.js")]
    [DataRow("**/gfx/*.gfx", "HKEY_LOCAL_MACHINE/gfx/foo.gfx")]
    [DataRow("**/gfx/**/*.gfx", "a_b/gfx/bar/foo.gfx")]
    [DataRow("**\\gfx\\**\\*.gfx", "a_b\\gfx\\bar\\foo.gfx")]
    [DataRow(@"/foo/bar!.baz", @"/foo/bar!.baz")]
    [DataRow(@"abc/**", @"abc/def/hij.txt")]
    [DataRow(@"abc/**", "abc/def")]
    [DataRow(@"**/some/path/some.file*.exe", "/some/path/some.file.exe")]
    [DataRow("literal1", "LITERAL1")]
    [DataRow("*ral*", "LITERAL1")]
    [DataRow("*Shock* 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
    [DataRow("*ave*2", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
    [DataRow("*ave 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
    public void Matches_AllMatches_ReturnsTrue(string pattern, string test)
    {
        var result = pattern.Matches(test);
        Assert.IsTrue(result);
    }

    [DataTestMethod]
    [DataRow("path/**/somefile.txt", "path/foo/bar/baz/somefile.txt")]
    //[DataRow("/**/file.*", "/file.txt")]
    [DataRow("**\\Shock* 12", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
    [DataRow("**\\*ave*2", "HKEY_LOCAL_MACHINE\\SOFTWARE\\Adobe\\Shockwave 12")]
    [DataRow("**/app*.js", "dist/app.a72ka8234.js")]
    //[DataRow(@"a/**/b", "a/b")]
    //[DataRow(@"/some/path/**/some.file*.exe", "/some/path/some.file.exe")]
    public void Matches_StarMatches_ReturnsTrue(string pattern, string test)
    {
        var result = pattern.Matches(test);
        Assert.IsTrue(result);
    }

}
