using BetterMoistureRedux.Config;
using HarmonyLib;
using Vintagestory.API.Common;

namespace BetterMoistureRedux;
public class BetterMoistureReduxModSystem : ModSystem {
	public static ILogger Logger { get; private set; }
	public static ICoreAPI Api { get; private set; }
	public static Harmony harmony { get; private set; }
	public static ModConfig config => ConfigLoader.Config;
	public override void Start(ICoreAPI api) {
		api.Logger.Debug("[BetterMoistureRedux] Start");
		base.StartPre(api);
		harmony = new Harmony(Mod.Info.ModID);
		Api = api;
		Logger = Mod.Logger;

		harmony.PatchAll();
	}

	public override void Dispose() {
		harmony?.UnpatchAll(Mod.Info.ModID);
		harmony = null;
		Logger = null;
		Api = null;
		base.Dispose();
	}
}