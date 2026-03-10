# Jellyfin Mapper Plugins

Two small Jellyfin plugins to remap metadata.

## Genre Mapper

Features:

- remap genres
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

Requires .NET SDK compatible with Jellyfin. Tested on 10.11.6 - I am not a coder at all, this was done with much help of AI. Feel free to fork and make it official or put in some advanced features.

Build plugins:

dotnet build Jellyfin.Plugin.GenreMapper
dotnet build Jellyfin.Plugin.RatingMapper
