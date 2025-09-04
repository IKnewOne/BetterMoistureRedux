using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BetterMoistureRedux.Config;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BetterMoistureRedux;

[HarmonyPatch(typeof(BlockEntityFarmland), "updateMoistureLevel")]
[HarmonyPatch(new Type[] { typeof(double), typeof(float), typeof(bool), typeof(ClimateCondition) })]
public static class UpdateMoistureTranspiler {

	public static float MoistureClampReplacement(float vanillaMath, float min, float max, float waterDistance) {
		float vanillaClamped = GameMath.Clamp(vanillaMath, min, max);
		float modClamped = ConfigLoader.Config.MoistureValues[GameMath.Clamp((int)waterDistance - 1, 0, ConfigLoader.Config.MoistureValues.Length - 1)];

		if (ConfigLoader.Config.overwriteLowerThanVanillaValues && vanillaClamped > modClamped) {
			return vanillaClamped;
		}

		return modClamped;
	}

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> DivertMoistureClampToCustomMethod(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
		var clampFloat = AccessTools.Method(
		typeof(GameMath), nameof(GameMath.Clamp), new[] { typeof(float), typeof(float), typeof(float) });

		var replacement = AccessTools.Method(typeof(UpdateMoistureTranspiler), nameof(MoistureClampReplacement));

		int clampIndex = 0;

		foreach (var code in instructions) {
			if (code.Calls(clampFloat)) {
				clampIndex++;

				if (clampIndex == 1) // only patch the first one
				{
					// push waterDistance (3rd argument, ldarg.2) as extra arg
					yield return new CodeInstruction(OpCodes.Ldarg_2);
					yield return new CodeInstruction(OpCodes.Call, replacement);
					continue;
				}
			}

			yield return code;
		}
	}
}