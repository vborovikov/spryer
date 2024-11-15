namespace Spryer.Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ExtensionTests
{
    [TestMethod]
    public void AsNVarChar_StringTooLong_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => "String too long".AsNVarChar(3, throwOnMaxLength: true));
    }

    [TestMethod]
    public void AsNVarChar_StringTooLong_Truncated()
    {
        var dbString = "String too long".AsNVarChar(3);

        Assert.AreEqual(3, dbString.Value?.Length);
    }
}
