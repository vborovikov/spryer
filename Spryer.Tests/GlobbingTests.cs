namespace Spryer.Tests;

extern alias Standalone;
using System.Buffers;

[TestClass]
public class GlobbingTests
{
    [TestMethod]
    public void CommonPrefixLength_EmptyArray_ReturnsZero()
    {
        string[] names = [];
        Assert.AreEqual(0, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_SingleElementArray_ReturnsZero()
    {
        string[] names = ["test"];
        Assert.AreEqual(0, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_NoCommonPrefix_ReturnsZero()
    {
        string[] names = ["test", "different", "another"];
        Assert.AreEqual(0, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_AllSame_ReturnsLength()
    {
        string[] names = ["test", "test", "test"];
        Assert.AreEqual(4, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_PartialCommonPrefix_ReturnsCommonLength()
    {
        string[] names = ["test_123", "test_456", "test_789"];
        Assert.AreEqual(5, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_MultipleDifferent_ReturnsCommonLength()
    {
        string[] names = ["test_123_test", "test_456_abc", "test_789_xyz"];
        Assert.AreEqual(5, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_WithEmptyString_ReturnsZero()
    {
        string[] names = ["test_123_test", "", "test_789_xyz"];
        Assert.AreEqual(0, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_MultipleShortString_ReturnsCommonLength()
    {
        string[] names = ["test", "tesa", "te"];
        Assert.AreEqual(2, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_MultipleShortString2_ReturnsCommonLength()
    {
        string[] names = ["test1", "tesa", "te"];
        Assert.AreEqual(2, names.CommonPrefixLength());
    }

    [TestMethod]
    public void CommonPrefixLength_NullInArray_ThrowsException()
    {
        string[] names = ["test", null!, "te"];
        Assert.ThrowsException<NullReferenceException>(() => names.CommonPrefixLength());
    }

    [TestMethod]
    public void IndexOfUnclosed_EmptySpan_ReturnsNegativeOne()
    {
        ReadOnlySpan<char> span = ReadOnlySpan<char>.Empty;
        var result = span.IndexOfUnclosed(',', '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfUnclosed_NoTargetValue_ReturnsNegativeOne()
    {
        ReadOnlySpan<char> span = "abc(def)ghi";
        var result = span.IndexOfUnclosed(',', '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfUnclosed_TargetValueOutsideOfEnclosure_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc,def(ghi)";
        var result = span.IndexOfUnclosed(',', '(', ')');
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void IndexOfUnclosed_TargetValueInsideEnclosure_ReturnsNegativeOne()
    {
        ReadOnlySpan<char> span = "abc(def,ghi)";
        var result = span.IndexOfUnclosed(',', '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfUnclosed_MultipleEnclosures_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc(def)ghi(jkl),mno";
        var result = span.IndexOfUnclosed(',', '(', ')');
        Assert.AreEqual(16, result);
    }

    [TestMethod]
    public void IndexOfUnclosed_NestedEnclosures_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc(def(ghi)),jkl";
        var result = span.IndexOfUnclosed(',', '(', ')');
        Assert.AreEqual(13, result);
    }

    [TestMethod]
    public void IndexOfUnclosed_UnclosedEnclosure_ReturnsNegativeOne()
    {
        ReadOnlySpan<char> span = "abc(def,ghi";
        var result = span.IndexOfUnclosed(',', '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfUnclosed_UnopenedEnclosure_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc)def,ghi";
        var result = span.IndexOfUnclosed(',', '(', ')');
        Assert.AreEqual(7, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_MultipleTargetValues_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc,def.ghi";
        var searchValues = SearchValues.Create(",.");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_MultipleTargetValuesInBrackets_ReturnsNegativeOne()
    {
        ReadOnlySpan<char> span = "abc,(def.ghi)";
        var searchValues = SearchValues.Create(".");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_TargetValueEqualsOpener_ReturnsNegativeIndex()
    {
        ReadOnlySpan<char> span = "abc(def)ghi(";
        var searchValues = SearchValues.Create("(");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_TargetValueEqualsCloser_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc(def)ghi)";
        var searchValues = SearchValues.Create(")");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(11, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_TargetValueEqualsOpenerInsideEnclosure_ReturnsNegativeOne()
    {
        ReadOnlySpan<char> span = "abc((def)ghi)";
        var searchValues = SearchValues.Create("(");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_TargetValueEqualsCloserInsideEnclosure_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc(def))ghi";
        var searchValues = SearchValues.Create(")");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(8, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_MultipleTargetValuesWithOpenerCloser_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "a(b)c,d.()e";
        var searchValues = SearchValues.Create(",.()");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(5, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_MultipleTargetValuesWithOpenerCloserInsideEnclosure_ReturnsNegativeOne()
    {
        ReadOnlySpan<char> span = "a(b(c,d.()e))f";
        var searchValues = SearchValues.Create(",.()");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_OnlyOpenerCloser_ReturnsNegativeIndex()
    {
        ReadOnlySpan<char> span = "()";
        var searchValues = SearchValues.Create("()");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(-1, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_MultipleTargetValuesNestedBrackets_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc(def(ghi)),.jkl";
        var searchValues = SearchValues.Create(",.");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(13, result);
    }

    [TestMethod]
    public void IndexOfAnyUnclosed_MultipleTargetValuesNestedBracketsInside_ReturnsCorrectIndex()
    {
        ReadOnlySpan<char> span = "abc(def(ghi),.).jkl";
        var searchValues = SearchValues.Create(",.");
        var result = span.IndexOfAnyUnclosed(searchValues, '(', ')');
        Assert.AreEqual(15, result);
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
        var result = Standalone.System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, test);
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
        var result = Standalone.System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, test);
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
        var result = Standalone.System.IO.Enumeration.FileSystemName.MatchesSimpleExpression(pattern, test);
        Assert.IsTrue(result);
    }
}
