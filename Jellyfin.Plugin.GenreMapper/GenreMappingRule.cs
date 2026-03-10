using System.Collections.Generic;

namespace Jellyfin.Plugin.GenreMapper;

public class GenreMappingRule
{
    public string From { get; set; } = string.Empty;
    public List<string> ToValues { get; set; } = new();
}
