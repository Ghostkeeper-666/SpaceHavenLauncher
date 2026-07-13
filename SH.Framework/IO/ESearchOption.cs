using System.IO;

namespace SH.Framework.IO;

public enum ESearchOption
{
    TopDir = SearchOption.TopDirectoryOnly,
    All = SearchOption.AllDirectories,
}

internal static class SearchOptionX
{
    internal static SearchOption ToSearchOption(this ESearchOption searchOption) => (SearchOption)searchOption;
}

