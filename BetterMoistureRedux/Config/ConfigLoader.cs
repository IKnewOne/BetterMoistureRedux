using System;
using Vintagestory.API.Common;

namespace BetterMoistureRedux.Config;

public class ConfigLoader : ModSystem
{
    private const string ConfigName = "BetterMoistureRedux.json";
    public static ModConfig Config { get; private set; }
    public override void StartPre(ICoreAPI api)
    {
        try
        {
            Config = api.LoadModConfig<ModConfig>(ConfigName);
            if (Config == null)
            {
                Config = new ModConfig();
                Mod.Logger.VerboseDebug("[BetterMoistureRedux]Config file not found, creating a new one...");
            }
            api.StoreModConfig(Config, ConfigName);
        } catch (Exception e) {
            Mod.Logger.Error("[BetterMoistureRedux] Failed to load config, you probably made a typo: {0}", e);
            Config = new ModConfig();
        }
    }
    
    public override void Dispose()
    {
        Config = null;
        base.Dispose();
    }
}