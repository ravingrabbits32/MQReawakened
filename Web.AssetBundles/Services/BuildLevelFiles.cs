﻿using Microsoft.Extensions.Logging;
using Server.Base.Core.Extensions;
using Server.Reawakened.Configs;
using Web.AssetBundles.Events.Arguments;
using Web.AssetBundles.Events;
using Web.AssetBundles.Models;
using Server.Base.Core.Abstractions;
using Web.AssetBundles.Extensions;

namespace Web.AssetBundles.Services;

public class BuildLevelFiles : IService
{
    private readonly AssetEventSink _eventSink;
    private readonly ILogger<BuildXmlFiles> _logger;
    private readonly ServerStaticConfig _sConfig;
    private readonly AssetBundleStaticConfig _config;

    public readonly Dictionary<string, string> LevelFiles;

    public BuildLevelFiles(AssetEventSink eventSink, ILogger<BuildXmlFiles> logger, ServerStaticConfig sConfig,
        AssetBundleStaticConfig config)
    {
        _eventSink = eventSink;
        _logger = logger;
        _sConfig = sConfig;
        _config = config;

        LevelFiles = new Dictionary<string, string>();
    }

    public void Initialize() => _eventSink.AssetBundlesLoaded += LoadXmlFiles;

    private void LoadXmlFiles(AssetBundleLoadEventArgs assetLoadEvent)
    {
        _logger.LogInformation("Reading Level Files From Bundles");

        LevelFiles.Clear();

        var assets = assetLoadEvent.InternalAssets
            .Select(x => x.Value)
            .Where(x => x.Type is AssetInfo.TypeAsset.Level)
            .ToArray();

        GetDirectory.OverwriteDirectory(_sConfig.LevelSaveDirectory);

        using var bar = new DefaultProgressBar(assets.Length, "Loading Level Files", _logger, _config);

        foreach (var asset in assets)
        {
            var text = asset.GetXmlData(bar);

            if (string.IsNullOrEmpty(text))
            {
                bar.SetMessage($"XML for {asset.Name} is empty! Skipping...");
                continue;
            }
            
            var path = Path.Join(_sConfig.LevelSaveDirectory, $"{asset.Name}.xml");

            bar.SetMessage($"Writing file to {path}");

            File.WriteAllText(path, text);

            LevelFiles.Add(asset.Name, path);

            bar.TickBar();
        }
    }
}