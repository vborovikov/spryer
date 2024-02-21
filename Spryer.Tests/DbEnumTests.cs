namespace Spryer.Tests;

using System;

[TestClass]
public class DbEnumTests
{
    [Flags]
    private enum TestEnum
    {
        None = 0,
        Value1 = 1 << 1,
        VAL1 = Value1,
        Value2 = 1 << 2,
        VAL2 = Value2,
        Value3 = 1 << 3,
        VAL3 = Value3,
    }

    [TestMethod]
    public void Initialize_DifferentSeparator_Set()
    {
        DbEnum<TestEnum>.Initialize('|');
        DbEnum<TestEnum> dbEnum = TestEnum.Value1 | TestEnum.Value3;

        Assert.AreEqual('|', EnumInfo<TestEnum>.ValueSeparator);
        Assert.AreEqual("VAL1|VAL3", dbEnum.ToString());
    }

    [TestMethod]
    public void Initialize_DefaultSeparator_Set()
    {
        DbEnum<TestEnum>.Initialize();

        Assert.AreEqual(',', EnumInfo<TestEnum>.ValueSeparator);
    }

    [TestMethod]
    public void Initialize_DefaultSeparator_Reset()
    {
        DbEnum<TestEnum>.Initialize('|');
        DbEnum<TestEnum>.Initialize();

        Assert.AreEqual(',', EnumInfo<TestEnum>.ValueSeparator);
    }
}
