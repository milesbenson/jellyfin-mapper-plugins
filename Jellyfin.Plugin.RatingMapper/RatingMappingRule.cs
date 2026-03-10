using System.Collections.Generic;

namespace Jellyfin.Plugin.RatingMapper;

public class RatingMappingRule
{
    public List<string> FromValues { get; set; } = new();
    public string To { get; set; } = string.Empty;
}
