namespace Spryer.Tests;

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
}
