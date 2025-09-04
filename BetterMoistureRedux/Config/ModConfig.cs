namespace BetterMoistureRedux.Config;
using Newtonsoft.Json;


public class ModConfig {
	public static string ConfigName = "BetterMoistureRedux.json";
	public static ModConfig Instance { get; set; } = new ModConfig();

	[JsonIgnore]
	public float[] MoistureValues {
		get {
			return new float[] { radius1 / 100f, radius2 / 100f, radius3 / 100f, radius4 / 100f };
		}
	}

	// Can only configure 1,2,3 radius. 4th is limited by described in main file
	public int radius1 = 100;
	public int radius2 = 80;
	public int radius3 = 60;
	public int radius4 = 0;

	public bool overwriteLowerThanVanillaValues = false;
	public bool removeFarmlandAdjacentWaterSound = true;
}
