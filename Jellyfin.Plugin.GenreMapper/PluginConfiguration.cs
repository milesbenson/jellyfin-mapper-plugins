using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.GenreMapper;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;
    public bool DryRun { get; set; } = true;
    public bool IgnoreCase { get; set; } = true;

    public List<string> LibraryNames { get; set; } = new();

    public string MappingsText { get; set; } =
@"Krimi => Crime
Komödie => Comedy
Familie => Family
Action & Adventure => Action|Adventure";
}
