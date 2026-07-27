# How to create a local nuget feed for offline work

Update the nuget store when new nugets or nugets with new versions are required.


# Get all required nugets (online)

`dotnet restore SH.slnx --packages C:\SH\LocalNugetCache`


# Copy (flatten) all nupkg files from LocalNugetCache to LocalNugetFeed:

On Windows:

`Get-ChildItem C:\SH\LocalNugetCache -Recurse -Filter *.nupkg | Copy-Item -Destination C:\SH\LocalNugetFeed`


# Use primarily the LocalNugetFeed defined in nuget.config

```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalNugetFeed" value="C:\SH\LocalNugetFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```
