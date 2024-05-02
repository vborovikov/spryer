namespace Spryer.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class EnumInfoTests
{
    public enum TestEnum
    {
        None = 0,
        Non = None,
        One,
        Two,
        Three,
        Tri = Three
    }

    [Flags]
    public enum TestFlag
    {
        None = 0,
        Non = None,
        One = 1 << 0,
        Two = 1 << 1,
        Three = 1 << 2,
        Tri = Three
    }

    [DataTestMethod]
    [DataRow(TestEnum.Three, nameof(TestEnum.Tri))]
    [DataRow(TestEnum.None, nameof(TestEnum.Non))]
    [DataRow(default(TestEnum), nameof(TestEnum.Non))]
    [DataRow(TestEnum.One, nameof(TestEnum.One))]
    [DataRow(TestEnum.Two, nameof(TestEnum.Two))]
    public void TryFormat_NoFlags_OneName(TestEnum value, string text)
    {
        Span<char> destination = stackalloc char[EnumInfo<TestEnum>.MaxLength];
        
        Assert.IsTrue(EnumInfo<TestEnum>.TryFormat(value, destination, out var charsWritten));
        Assert.AreEqual(text, destination[..charsWritten].ToString());
    }


    [DataTestMethod]
    [DataRow(TestFlag.One | TestFlag.Two, $"{nameof(TestFlag.One)},{nameof(TestFlag.Two)}")]
    [DataRow(TestFlag.One | TestFlag.Two | TestFlag.Three, $"{nameof(TestFlag.One)},{nameof(TestFlag.Two)},{nameof(TestFlag.Tri)}")]
    [DataRow(TestFlag.None, nameof(TestFlag.Non))]
    [DataRow(default(TestFlag), nameof(TestFlag.Non))]
    [DataRow(TestFlag.None | TestFlag.Two, nameof(TestFlag.Two))]
    public void TryFormat_Flags_MultipleNames(TestFlag value, string text)
    {
        Span<char> destination = stackalloc char[EnumInfo<TestFlag>.MaxLength];

        Assert.IsTrue(EnumInfo<TestFlag>.TryFormat(value, destination, out var charsWritten));
        Assert.AreEqual(text, destination[..charsWritten].ToString());
    }

    [TestMethod]
    public void TryParse_NonDefaultSeparator_Parsed()
    {
        EnumInfo<TestFlag>.ValueSeparator = '|';
        var value = TestFlag.One | TestFlag.Two;
        var valueStr = EnumInfo<TestFlag>.ToString(value);

        Assert.AreEqual("One|Two", valueStr);
        Assert.IsTrue(EnumInfo<TestFlag>.TryParse(valueStr, ignoreCase: true, out var result));
        Assert.AreEqual(value, result);
    }
}