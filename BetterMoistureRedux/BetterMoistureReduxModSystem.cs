using System;
using System.Linq;
using System.Reflection;
using BetterMoistureRedux.Config;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

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

		var originalUpdateMoistureLevel = typeof(BlockEntityFarmland).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m => m.Name == "updateMoistureLevel" && m.GetParameters().Length == 4);

		harmony.Patch(originalUpdateMoistureLevel, prefix: typeof(BetterMoistureReduxModSystem).GetMethod(nameof(updateMoistureLevel)));
	}

	// I hate transpilers. We'll do prefix. Should transpile the first clamp but alas
	public static bool updateMoistureLevel(BlockEntityFarmland __instance, ref bool __result, double totalDays, float waterDistance, bool skyExposed, ClimateCondition baseClimate = null) {
		double lastMoistureLevelUpdateTotalDays = __instance.GetField<double>("lastMoistureLevelUpdateTotalDays");
		BlockPos Pos = __instance.Pos;
		Vec3d tmpPos = __instance.GetField<Vec3d>("tmpPos");
		float moistureLevel = __instance.GetField<float>("moistureLevel");
		float totalHoursWaterRetention = __instance.GetField<float>("totalHoursWaterRetention");
		BlockFarmland blockFarmland = __instance.GetField<BlockFarmland>("blockFarmland");
		// Original code

		tmpPos.Set(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5);
		// CHANGED
		float minMoisture = config.MoistureValues[GameMath.Clamp((int)waterDistance - 1, 0, config.MoistureValues.Length - 1)];
		//

		if (lastMoistureLevelUpdateTotalDays > Api.World.Calendar.TotalDays) {
			// We need to rollback time when the blockEntity saved date is ahead of the calendar date: can happen if a schematic is imported
			lastMoistureLevelUpdateTotalDays = Api.World.Calendar.TotalDays;
			return false;
		}

		double hoursPassed = Math.Min((totalDays - lastMoistureLevelUpdateTotalDays) * Api.World.Calendar.HoursPerDay, totalHoursWaterRetention);
		if (hoursPassed < 0.03f) {
			// Get wet from a water source
			moistureLevel = Math.Max(moistureLevel, minMoisture);

			return false;
		}

		// Dry out
		moistureLevel = Math.Max(minMoisture, moistureLevel - (float)hoursPassed / totalHoursWaterRetention);

		// Get wet from all the rainfall since last update
		if (skyExposed) {
			if (baseClimate == null && hoursPassed > 0)
				baseClimate = Api.World.BlockAccessor.GetClimateAt(Pos, EnumGetClimateMode.WorldGenValues, totalDays - hoursPassed / Api.World.Calendar.HoursPerDay / 2);
			while (hoursPassed > 0) {
				double rainLevel = blockFarmland.wsys.GetPrecipitation(Pos, totalDays - hoursPassed / Api.World.Calendar.HoursPerDay, baseClimate);
				moistureLevel = GameMath.Clamp(moistureLevel + (float)rainLevel / 3f, 0, 1);
				hoursPassed--;
			}
		}

		// CHANGED
		__instance.SetField("lastMoistureLevelUpdateTotalDays", totalDays);
		__instance.SetField("moistureLevel", moistureLevel);
		__result = true;
		//

		return false;
	}

	public override void Dispose() {
		harmony?.UnpatchAll(Mod.Info.ModID);
		harmony = null;
		Logger = null;
		Api = null;
		base.Dispose();
	}
}


public static class HarmonyReflectionExtensions {
	public static T GetField<T>(this object instance, string fieldName) {
		return (T)AccessTools.Field(instance.GetType(), fieldName).GetValue(instance);
	}
	public static void SetField(this object instance, string fieldName, object setVal) {
		AccessTools.Field(instance.GetType(), fieldName).SetValue(instance, setVal);
	}
}