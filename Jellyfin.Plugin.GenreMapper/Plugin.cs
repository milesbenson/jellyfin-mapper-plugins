using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.GenreMapper;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILibraryManager _libraryManager;

    public static Plugin? Instance { get; private set; }

    public override string Name => "Genre Mapper";

    public override Guid Id => Guid.Parse("1d59c81d-50a4-4b28-aed7-86a4fd0c8c91");

    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILibraryManager libraryManager)
        : base(applicationPaths, xmlSerializer)
    {
        _libraryManager = libraryManager;
        Instance = this;
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "genremapper",
                EmbeddedResourcePath = "Jellyfin.Plugin.GenreMapper.Web.configPage.html"
            }
        };
    }

    public List<GenreMappingRule> GetMappingRules()
    {
        return GenreMappingParser.Parse(Configuration.MappingsText);
    }

    public List<string> GetAvailableLibraryNames()
    {
        return _libraryManager.GetVirtualFolders()
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
