namespace Spryer.Tests;

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
}
