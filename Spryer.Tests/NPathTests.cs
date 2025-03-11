namespace Spryer.Tests;

using BuildTasks;

[TestClass]
public class NPathTests
{
    [DataTestMethod]
    [DataRow(@"C:\", @"C:\", @".")]
    [DataRow(@"C:\a", @"C:\a\", @".")]
    [DataRow(@"C:\A", @"C:\a\", @".")]
    [DataRow(@"C:\a\", @"C:\a", @".")]
    [DataRow(@"C:\", @"C:\b", @"b")]
    [DataRow(@"C:\a", @"C:\b", @"..\b")]
    [DataRow(@"C:\a", @"C:\b\", @"..\b\")]
    [DataRow(@"C:\a\b", @"C:\a", @"..")]
    [DataRow(@"C:\a\b", @"C:\a\", @"..")]
    [DataRow(@"C:\a\b\", @"C:\a", @"..")]
    [DataRow(@"C:\a\b\", @"C:\a\", @"..")]
    [DataRow(@"C:\a\b\c", @"C:\a\b", @"..")]
    [DataRow(@"C:\a\b\c", @"C:\a\b\", @"..")]
    [DataRow(@"C:\a\b\c", @"C:\a", @"..\..")]
    [DataRow(@"C:\a\b\c", @"C:\a\", @"..\..")]
    [DataRow(@"C:\a\b\c\", @"C:\a\b", @"..")]
    [DataRow(@"C:\a\b\c\", @"C:\a\b\", @"..")]
    [DataRow(@"C:\a\b\c\", @"C:\a", @"..\..")]
    [DataRow(@"C:\a\b\c\", @"C:\a\", @"..\..")]
    [DataRow(@"C:\a\", @"C:\b", @"..\b")]
    [DataRow(@"C:\a", @"C:\a\b", @"b")]
    [DataRow(@"C:\a", @"C:\A\b", @"b")]
    [DataRow(@"C:\a", @"C:\b\c", @"..\b\c")]
    [DataRow(@"C:\a\", @"C:\a\b", @"b")]
    [DataRow(@"C:\", @"D:\", @"D:\")]
    [DataRow(@"C:\", @"D:\b", @"D:\b")]
    [DataRow(@"C:\", @"D:\b\", @"D:\b\")]
    [DataRow(@"C:\a", @"D:\b", @"D:\b")]
    [DataRow(@"C:\a\", @"D:\b", @"D:\b")]
    [DataRow(@"C:\ab", @"C:\a", @"..\a")]
    [DataRow(@"C:\a", @"C:\ab", @"..\ab")]
    [DataRow(@"C:\", @"\\LOCALHOST\Share\b", @"\\LOCALHOST\Share\b")]
    [DataRow(@"\\LOCALHOST\Share\a", @"\\LOCALHOST\Share\b", @"..\b")]
    public void GetRelativePath_Windows(string relativeTo, string path, string expected)
    {
        var result = NPath.GetRelativePath(relativeTo, path);
        Assert.AreEqual(expected, result);

        // Check that we get the equivalent path when the result is combined with the sources
        Assert.AreEqual(Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(Path.Combine(Path.GetFullPath(relativeTo), result)).TrimEnd(Path.DirectorySeparatorChar),
            ignoreCase: true);
    }
}
