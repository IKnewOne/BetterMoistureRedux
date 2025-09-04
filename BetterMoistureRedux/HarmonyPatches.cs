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
	public static ModConfig config = ModConfig.Instance;

	public static float MoistureClampReplacement(float vanillaMath, float min, float max, float waterDistance) {
		float vanillaClamped = GameMath.Clamp(vanillaMath, min, max);
		float modClamped = BetterMoistureReduxModSystem.config.MoistureValues[GameMath.Clamp((int)waterDistance - 1, 0, config.MoistureValues.Length - 1)];

		if (config.overwriteLowerThanVanillaValues && vanillaClamped > modClamped) {
			return vanillaClamped;
		}

		return modClamped;
	}

	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> DivertMoistureClampToCustomMethod(IEnumerable<CodeInstruction> instructions) {
		var clampMethod = AccessTools.Method(
			typeof(GameMath), nameof(GameMath.Clamp), new[] { typeof(float), typeof(float), typeof(float) }
		);

		var replacementMethod = AccessTools.Method(
			typeof(UpdateMoistureTranspiler), nameof(MoistureClampReplacement)
		);

		return new CodeMatcher(instructions)
			.MatchStartForward(new CodeMatch(c => c.Calls(clampMethod)))
			.InsertAndAdvance(
				new CodeInstruction(OpCodes.Ldarg_2), // push waterDistance as extra argument
				new CodeInstruction(OpCodes.Call, replacementMethod)
			)
			.RemoveInstructions(1) // remove original Clamp call
			.InstructionEnumeration();
	}
}

[HarmonyPatchCategory("removeFarmlandWaterSloshing")]
[HarmonyPatch(typeof(BlockWater), nameof(BlockWater.GetAmbientSoundStrength))]
public static class RemoveFarmlandAdjacentWaterSound {
	[HarmonyPrefix]
	public static bool Prefix(ref BlockWater __instance, ref float __result, IWorldAccessor world, BlockPos pos) {
		bool foundFarmland = false;
		world.BlockAccessor.WalkBlocks(
			new BlockPos(pos.X - 1, pos.Y, pos.Z - 1),
			new BlockPos(pos.X + 1, pos.Y, pos.Z + 1),
			(block, x, y, z) => {
				if (block is BlockFarmland) {
					foundFarmland = true;
					return;
				}
			}
		);

		if (foundFarmland) {
			__result = 0f;
			return false;
		}


		return true;
	}
}