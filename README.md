# Jellyfin Mapper Plugins

Two small Jellyfin plugins to normalize metadata.

## Genre Mapper

Features:

- map genres
- split genres (`Action & Adventure => Action|Adventure`)
- remove genres (`Talk =>`)
- optional library filter
- DryRun mode
- statistics in logs

## Rating Mapper

Features:

- map ratings
- `<empty>` support
- alias values (`PG, TV-PG => FSK-6`)
- library filter
- unknown rating detection
- statistics in logs

## Build

Requires .NET SDK compatible with Jellyfin.

Build plugins:

dotnet build Jellyfin.Plugin.GenreMapper

dotnet build Jellyfin.Plugin.RatingMapper

## Examples GenreMapper

`Abenteuer => Adventure
Dokumentarfilm => Documentary
Familie => Family
Historie => History
Komödie => Comedy
Krimi => Crime
Liebesfilm => Romance
Kriegsfilm => War
Comendy => Comedy
Science Fiction => Sci-Fi
Sci-Fi & Fantasy => Sci-Fi|Fantasy
War & Politics => War
Suspense =>
Talk =>
Soap =>
TV-Film =>
Action & Adventure => Action|Adventure`

## Examples RatingMapper

`TV-G, TV-Y, TP, G, ES-TP, SG-PG, KR-ALL, AL, ATP, DE-0, 0+ => FSK-0`
`TV-PG, TV-Y7, PG, -10, 10, AT-6, FR-10, 6, 9, 7, C8, AU-PG, T, DE-6, 6+ => FSK-6`
`14+, 14, 12, -12, 13+, L, BR-14, A, AT-12, SG-PG13, +13, CH-12, FR-12, NL-12, UA13+, MX-C, CA-14+, ES-12, FI-K12, NL-9, ES-13, IE-12, M12, BR-12, DK-11, DE-12, 12+, PG-13, 11+ => FSK-12`
`TV-MA, TV-14, 16, -16, 15, MA15+, BR-16, M, SG-NC16, ES-16, AU-M, M16, KR-15, NL-16, AU-MA 15+, PL-16, AT-16, 15+, UA16+, IN-U/A 16+, IL-15+, CH-16, NL-14, AU-MA15+, GB-15, DE-16, 16+, RU-16+, R, TW-15+ => FSK-16`
`18, SG-M18, SG-R21, ES-18, KR-19, 19, TR-18+, +18, IN-A, PL-18, FR-18, DE-18, 18+, GB-18 => FSK-18`
`Unrated, <empty> => NR`

## Notes

I am not a coder at all, this was done with AI. Tested on 10.11.6 with a very large library without any issues.
Feel free to fork and make it official or add advanced features. 
