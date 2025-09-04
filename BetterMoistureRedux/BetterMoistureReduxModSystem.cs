using System;
using BetterMoistureRedux.Config;
using HarmonyLib;
using Vintagestory.API.Common;

namespace BetterMoistureRedux;
public class BetterMoistureReduxModSystem : ModSystem {
	public static ILogger Logger { get; private set; }
	public static ICoreAPI Api { get; private set; }
	public static Harmony harmony { get; private set; }
	public static ModConfig config;

	public override void StartPre(ICoreAPI api) {
		try {
			config = api.LoadModConfig<ModConfig>(ModConfig.ConfigName);
			if (config == null) {
				config = new ModConfig();
				Mod.Logger.VerboseDebug("[BetterMoistureRedux] Config file not found, creating a new one...");
			}
			api.StoreModConfig(config, ModConfig.ConfigName);
		} catch (Exception e) {
			Mod.Logger.Error("[BetterMoistureRedux] Failed to load config, you probably made a typo: {0}", e);
			config = new ModConfig();
		}
	}


	public override void Start(ICoreAPI api) {
		api.Logger.Debug("[BetterMoistureRedux] Start");
		base.StartPre(api);
		harmony = new Harmony(Mod.Info.ModID);
		Api = api;
		Logger = Mod.Logger;

		harmony.PatchAllUncategorized();

		if (config.removeFarmlandAdjacentWaterSound) {
			harmony.PatchCategory("removeFarmlandWaterSloshing");
		}
	}

	public override void Dispose() {
		harmony?.UnpatchAll(Mod.Info.ModID);
		config = null;
		harmony = null;
		Logger = null;
		Api = null;
		base.Dispose();
	}
}
