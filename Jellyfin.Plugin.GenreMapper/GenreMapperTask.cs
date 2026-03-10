using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GenreMapper;

public class GenreMapperTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<GenreMapperTask> _logger;

    public GenreMapperTask(
        ILibraryManager libraryManager,
        ILogger<GenreMapperTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public string Name => "Genre Mapper";
    public string Key => "GenreMapperTask";
    public string Description => "Mapped Genres für Filme und Serien anwenden";
    public string Category => "Genre Mapper";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance ?? throw new InvalidOperationException("Plugin.Instance is null.");
        var config = plugin.Configuration;

        if (!config.Enabled)
        {
            _logger.LogInformation("Genre Mapper ist deaktiviert.");
            return Task.CompletedTask;
        }

        var rules = plugin.GetMappingRules();
        if (rules.Count == 0)
        {
            _logger.LogInformation("Genre Mapper: keine Regeln vorhanden.");
            return Task.CompletedTask;
        }

        var comparer = config.IgnoreCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var map = new Dictionary<string, List<string>>(comparer);

        foreach (var rule in rules)
        {
            if (!string.IsNullOrWhiteSpace(rule.From) && !map.ContainsKey(rule.From))
            {
                map[rule.From] = rule.ToValues ?? new List<string>();
            }
        }

        var selectedLibraries = config.LibraryNames ?? new List<string>();
        var hasLibraryFilter = selectedLibraries.Count > 0;
        var allowedLibraries = new HashSet<string>(selectedLibraries, StringComparer.OrdinalIgnoreCase);

        List<BaseItem> allItems;

        if (hasLibraryFilter)
        {
            var virtualFolders = _libraryManager
                .GetVirtualFolders()
                .Where(v => allowedLibraries.Contains(v.Name))
                .ToList();

            _logger.LogInformation(
                "Genre Mapper Start: Bibliotheksfilter aktiv. Ausgewählt={Selected}. Gefunden={Matched}",
                string.Join(", ", selectedLibraries),
                string.Join(", ", virtualFolders.Select(v => v.Name)));

            var collected = new Dictionary<Guid, BaseItem>();

            foreach (var folder in virtualFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Guid.TryParse(folder.ItemId, out var folderGuid))
                {
                    _logger.LogWarning(
                        "Genre Mapper: Bibliothek \"{LibraryName}\" hat keine gültige ItemId: {ItemId}",
                        folder.Name,
                        folder.ItemId);
                    continue;
                }

                var libraryItems = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                    Recursive = true,
                    AncestorIds = new[] { folderGuid }
                });

                foreach (var item in libraryItems)
                {
                    if (!collected.ContainsKey(item.Id))
                    {
                        collected[item.Id] = item;
                    }
                }
            }

            allItems = collected.Values.ToList();
        }
        else
        {
            allItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                Recursive = true
            }).ToList();
        }

        _logger.LogInformation(
            "Genre Mapper Start: Kandidaten={Candidates}, DryRun={DryRun}, BibliotheksfilterAktiv={HasLibraryFilter}",
            allItems.Count,
            config.DryRun,
            hasLibraryFilter);

        var changed = 0;
        var scanned = 0;
        var removedGenres = 0;
        var splitGenres = 0;

        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scanned++;

            var oldGenres = item.Genres?.ToArray() ?? Array.Empty<string>();

            if (oldGenres.Length == 0)
            {
                progress.Report((double)scanned / Math.Max(allItems.Count, 1) * 100);
                continue;
            }

            var newGenres = new List<string>();
            var itemChanged = false;
            var itemRemoved = 0;
            var itemSplit = 0;

            foreach (var genre in oldGenres)
            {
                if (map.TryGetValue(genre, out var mappedValues))
                {
                    itemChanged = true;

                    if (mappedValues.Count == 0)
                    {
                        itemRemoved++;
                        continue;
                    }

                    if (mappedValues.Count > 1)
                    {
                        itemSplit++;
                    }

                    foreach (var mapped in mappedValues)
                    {
                        if (!string.IsNullOrWhiteSpace(mapped))
                        {
                            newGenres.Add(mapped);
                        }
                    }
                }
                else
                {
                    newGenres.Add(genre);
                }
            }

            newGenres = newGenres
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(comparer)
                .ToList();

            var sameValues =
                oldGenres.Length == newGenres.Count &&
                oldGenres.SequenceEqual(newGenres, comparer);

            if (!itemChanged && sameValues)
            {
                progress.Report((double)scanned / Math.Max(allItems.Count, 1) * 100);
                continue;
            }

            changed++;
            removedGenres += itemRemoved;
            splitGenres += itemSplit;

            if (config.DryRun)
            {
                _logger.LogInformation(
                    "[DRY-RUN] {Name}: {Old} => {New}",
                    item.Name,
                    string.Join(", ", oldGenres),
                    string.Join(", ", newGenres));
            }
            else
            {
                item.Genres = newGenres.ToArray();

                item.UpdateToRepositoryAsync(
                    ItemUpdateType.MetadataEdit,
                    cancellationToken)
                    .GetAwaiter()
                    .GetResult();

                _logger.LogInformation(
                    "Geändert: {Name}: {Old} => {New}",
                    item.Name,
                    string.Join(", ", oldGenres),
                    string.Join(", ", newGenres));
            }

            progress.Report((double)scanned / Math.Max(allItems.Count, 1) * 100);
        }

        _logger.LogInformation(
            "Genre Mapper fertig. Kandidaten={Candidates}, Geändert={Changed}, Entfernt={RemovedGenres}, Gesplittet={SplitGenres}, DryRun={DryRun}",
            allItems.Count,
            changed,
            removedGenres,
            splitGenres,
            config.DryRun);

        return Task.CompletedTask;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }
}
