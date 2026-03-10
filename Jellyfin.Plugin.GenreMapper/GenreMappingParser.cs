using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.GenreMapper;

public static class GenreMappingParser
{
    public static List<GenreMappingRule> Parse(string? text)
    {
        var rules = new List<GenreMappingRule>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return rules;
        }

        var lines = text.Split('\n');

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split("=>", 2, StringSplitOptions.None);

            if (parts.Length != 2)
            {
                continue;
            }

            var from = parts[0].Trim();
            var right = parts[1].Trim();

            if (from.Length == 0)
            {
                continue;
            }

            var toValues = right
                .Split('|', StringSplitOptions.None)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();

            rules.Add(new GenreMappingRule
            {
                From = from,
                ToValues = toValues
            });
        }

        return rules;
    }

    public static List<string> ParseLibraryNames(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        return text
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
