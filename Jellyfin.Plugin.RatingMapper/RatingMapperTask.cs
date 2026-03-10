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

namespace Jellyfin.Plugin.RatingMapper;

public class RatingMapperTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<RatingMapperTask> _logger;

    public RatingMapperTask(
        ILibraryManager libraryManager,
        ILogger<RatingMapperTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    public string Name => "Rating Mapper";
    public string Key => "RatingMapperTask";
    public string Description => "Mapped Ratings für Filme und Serien anwenden";
    public string Category => "Rating Mapper";

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance ?? throw new InvalidOperationException("Plugin.Instance is null.");
        var config = plugin.Configuration;

        if (!config.Enabled)
        {
            _logger.LogInformation("Rating Mapper ist deaktiviert.");
            return Task.CompletedTask;
        }

        var rules = plugin.GetMappingRules();

        if (rules.Count == 0)
        {
            _logger.LogInformation("Rating Mapper: keine Regeln vorhanden.");
            return Task.CompletedTask;
        }

        var comparer = config.IgnoreCase
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var map = new Dictionary<string, string>(comparer);
        string? emptyTarget = null;
        var hasEmptyRule = false;

        foreach (var rule in rules)
        {
            foreach (var fromValue in rule.FromValues)
            {
                if (string.Equals(fromValue, "<empty>", StringComparison.OrdinalIgnoreCase))
                {
                    emptyTarget = rule.To ?? string.Empty;
                    hasEmptyRule = true;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(fromValue) && !map.ContainsKey(fromValue))
                {
                    map[fromValue] = rule.To ?? string.Empty;
                }
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
                "Rating Mapper Start: Bibliotheksfilter aktiv. Ausgewählt={Selected}. Gefunden={Matched}",
                string.Join(", ", selectedLibraries),
                string.Join(", ", virtualFolders.Select(v => v.Name)));

            var collected = new Dictionary<Guid, BaseItem>();

            foreach (var folder in virtualFolders)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Guid.TryParse(folder.ItemId, out var folderGuid))
                {
                    _logger.LogWarning(
                        "Rating Mapper: Bibliothek \"{LibraryName}\" hat keine gültige ItemId: {ItemId}",
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
            "Rating Mapper Start: Kandidaten={Candidates}, DryRun={DryRun}, BibliotheksfilterAktiv={HasLibraryFilter}",
            allItems.Count,
            config.DryRun,
            hasLibraryFilter);

        var changed = 0;
        var scanned = 0;
        var emptyMapped = 0;
        var removedRatings = 0;
        var unknownRatings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in allItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scanned++;

            var oldRatingRaw = item.OfficialRating ?? string.Empty;
            var oldRating = oldRatingRaw.Trim();
            var isEmpty = string.IsNullOrWhiteSpace(oldRating);

            string newRating;
            var hasMapping = false;

            if (isEmpty)
            {
                if (!hasEmptyRule)
                {
                    progress.Report((double)scanned / Math.Max(allItems.Count, 1) * 100);
                    continue;
                }

                newRating = emptyTarget ?? string.Empty;
                hasMapping = true;
            }
            else if (map.TryGetValue(oldRating, out var mapped))
            {
                newRating = mapped ?? string.Empty;
                hasMapping = true;
            }
            else
            {
                unknownRatings.Add(oldRating);
                progress.Report((double)scanned / Math.Max(allItems.Count, 1) * 100);
                continue;
            }

            if (!hasMapping)
            {
                progress.Report((double)scanned / Math.Max(allItems.Count, 1) * 100);
                continue;
            }

            var normalizedNew = (newRating ?? string.Empty).Trim();
            var sameValue = comparer.Equals(oldRating, normalizedNew);

            if (sameValue)
            {
                progress.Report((double)scanned / Math.Max(allItems.Count, 1) * 100);
                continue;
            }

            changed++;

            if (isEmpty)
            {
                emptyMapped++;
            }

            if (!isEmpty && string.IsNullOrWhiteSpace(normalizedNew))
            {
                removedRatings++;
            }

            var oldDisplay = string.IsNullOrWhiteSpace(oldRating) ? "<empty>" : oldRating;
            var newDisplay = string.IsNullOrWhiteSpace(normalizedNew) ? "<empty>" : normalizedNew;

            if (config.DryRun)
            {
                _logger.LogInformation(
                    "[DRY-RUN] {Name}: {Old} => {New}",
                    item.Name,
                    oldDisplay,
                    newDisplay);
            }
            else
            {
                item.OfficialRating = string.IsNullOrWhiteSpace(normalizedNew) ? null : normalizedNew;

                item.UpdateToRepositoryAsync(
                    ItemUpdateType.MetadataEdit,
                    cancellationToken)
                    .GetAwaiter()
                    .GetResult();

                _logger.LogInformation(
                    "Geändert: {Name}: {Old} => {New}",
                    item.Name,
                    oldDisplay,
                    newDisplay);
            }

            progress.Report((double)scanned / Math.Max(allItems.Count, 1) * 100);
        }

        if (unknownRatings.Count > 0)
        {
            _logger.LogInformation(
                "Unknown ratings detected: {Ratings}",
                string.Join(", ", unknownRatings.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)));
        }

        _logger.LogInformation(
            "Rating Mapper fertig. Kandidaten={Candidates}, Geändert={Changed}, EmptyMapped={EmptyMapped}, Entfernt={RemovedRatings}, UnknownRatings={UnknownCount}, DryRun={DryRun}",
            allItems.Count,
            changed,
            emptyMapped,
            removedRatings,
            unknownRatings.Count,
            config.DryRun);

        return Task.CompletedTask;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Array.Empty<TaskTriggerInfo>();
    }
}
