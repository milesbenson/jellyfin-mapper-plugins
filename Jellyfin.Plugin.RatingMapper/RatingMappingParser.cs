using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Plugin.RatingMapper;

public static class RatingMappingParser
{
    public static List<RatingMappingRule> Parse(string? text)
    {
        var rules = new List<RatingMappingRule>();

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

            var left = parts[0].Trim();
            var right = parts[1].Trim();

            var fromValues = left
                .Split(',', StringSplitOptions.None)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();

            if (fromValues.Count == 0)
            {
                continue;
            }

            rules.Add(new RatingMappingRule
            {
                FromValues = fromValues,
                To = right
            });
        }

        return rules;
    }
}
