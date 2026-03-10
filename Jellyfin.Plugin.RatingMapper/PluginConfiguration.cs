using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.RatingMapper;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;
    public bool DryRun { get; set; } = true;
    public bool IgnoreCase { get; set; } = true;

    public List<string> LibraryNames { get; set; } = new();

    public string MappingsText { get; set; } =
@"<empty> => NR
none, Unrated => NR
TV-PG, PG => FSK-6
PG-13 => FSK-12";
}
