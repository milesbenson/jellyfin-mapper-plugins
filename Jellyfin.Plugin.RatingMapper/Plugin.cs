using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.RatingMapper;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILibraryManager _libraryManager;

    public static Plugin? Instance { get; private set; }

    public override string Name => "Rating Mapper";

    public override Guid Id => Guid.Parse("3b6f5dd1-5b6c-4cf3-b32f-4ab0d5f19d42");

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
                Name = "ratingmapper",
                EmbeddedResourcePath = "Jellyfin.Plugin.RatingMapper.Web.configPage.html"
            }
        };
    }

    public List<RatingMappingRule> GetMappingRules()
    {
        return RatingMappingParser.Parse(Configuration.MappingsText);
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
