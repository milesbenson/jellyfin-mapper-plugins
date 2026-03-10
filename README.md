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

Requires .NET SDK compatible with Jellyfin. 

Build plugins:

dotnet build Jellyfin.Plugin.GenreMapper
dotnet build Jellyfin.Plugin.RatingMapper

## Notes

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

## Notes

I am not a coder at all, this was done with AI. Tested on 10.11.6 with a very large library without any issues.
Feel free to fork and make it official or add advanced features. 
