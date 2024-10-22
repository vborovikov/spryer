namespace Spryer.Tests;

using System.ComponentModel;
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

    [Flags]
    private enum TestSeparator
    {
        None = 0,
        Non = None,
        One = 1 << 0,
        Two = 1 << 1,
        Three = 1 << 2,
        Tri = Three
    }

    [TestMethod]
    public void TryParse_NonDefaultSeparator_Parsed()
    {
        EnumInfo<TestSeparator>.ValueSeparator = '|';
        var value = TestSeparator.One | TestSeparator.Two;
        var valueStr = EnumInfo<TestSeparator>.ToString(value);

        Assert.AreEqual("One|Two", valueStr);
        Assert.IsTrue(EnumInfo<TestSeparator>.TryParse(valueStr, ignoreCase: true, out var result));
        Assert.AreEqual(value, result);
    }

    [Flags]
    private enum TestAmbient
    {
        None = 0,
        [AmbientValue("N1")]
        Name1 = 1,
        Name2 = 2,
        Name4 = 8,
        N2 = Name2,
        [AmbientValue("N3")]
        Name3 = 4,
    }

    [TestMethod]
    public void TryParse_AmbientNames_Parsed()
    {
        EnumInfo<TestAmbient>.ValueSeparator = '|';
        var parsed = EnumInfo<TestAmbient>.TryParse("N1|n3", ignoreCase: true, out var result);

        Assert.IsTrue(parsed);
        Assert.AreEqual(TestAmbient.Name1 | TestAmbient.Name3, result);
    }

    [TestMethod]
    public void TryParse_MixedNames_Parsed()
    {
        EnumInfo<TestAmbient>.ValueSeparator = ':';
        var parsed = EnumInfo<TestAmbient>.TryParse("N1:n2", ignoreCase: true, out var result);

        Assert.IsTrue(parsed);
        Assert.AreEqual(TestAmbient.Name1 | TestAmbient.Name2, result);
    }
}