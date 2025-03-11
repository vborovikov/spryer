namespace Spryer.BuildTasks;

using System;
using System.IO;
using System.Linq;

static class NPath
{
    private static readonly string DirectorySeparatorStr = $"{Path.DirectorySeparatorChar}";
    private static readonly char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <summary>
    /// Rebases file with <paramref name="path"/> to the folder specified by <paramref name="relativeTo"/>.
    /// </summary>
    /// <param name="path">Full file path (absolute)</param>
    /// <param name="relativeTo">Full base directory path (absolute) the result should be relative to. This path is always considered to be a directory.</param>
    /// <returns>Relative path to file with respect to <paramref name="relativeTo"/></returns>
    /// <remarks>Paths are resolved by calling the <seealso cref="System.IO.Path.GetFullPath(string)"/> method before calculating the difference. This will resolve relative path fragments:
    /// <code>
    /// "c:\test\..\test2" => "c:\test2"
    /// </code>
    /// These path framents are expected to be created by concatenating a root folder with a relative path such as this:
    /// <code>
    /// var baseFolder = @"c:\test\";
    /// var virtualPath = @"..\test2";
    /// var fullPath = System.IO.Path.Combine(baseFolder, virtualPath);
    /// </code>
    /// The default file path for the current executing environment will be used for the base resolution for this operation, which may not be appropriate if the input paths are fully relative or relative to different
    /// respective base paths. For this reason we should attempt to resolve absolute input paths <i>before</i> passing through as arguments to this method.
    /// </remarks>
    public static string GetRelativePath(string relativeTo, string path)
    {
        var itemPath = Path.GetFullPath(path);
        var baseDirPath = Path.GetFullPath(relativeTo);
        var isDirectory = path.EndsWith(DirectorySeparatorStr, StringComparison.Ordinal);

        var p1 = itemPath.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);
        var p2 = baseDirPath.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);

        var i = 0;
        for (; i < p1.Length && i < p2.Length; i++)
        {
            if (string.Compare(p1[i], p2[i], true) != 0)
            {
                // case insensitive match
                break;
            }
        }
        if (i == 0)
        {
            // cannot make relative path, for example if resides on different drive
            return itemPath;
        }

        var r = string.Join(DirectorySeparatorStr, Enumerable.Repeat("..", p2.Length - i).Concat(p1.Skip(i).Take(p1.Length - i)));
        if (string.IsNullOrEmpty(r))
        {
            return ".";
        }
        else if (isDirectory && p1.Length >= p2.Length)
        {
            // only append on forward traversal, to match .Net Standard Implementation of System.IO.Path.GetRelativePath
            r += DirectorySeparatorStr;
        }

        return r;
    }
}
