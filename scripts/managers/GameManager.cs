using Godot;
using Godot.Collections;

namespace CyberPond;

/// Game startup orchestrator. Loads configs, initializes managers, restores or creates save.
public partial class GameManager : Node
{
	private const string ConfigPath = "res://configs/game_config.json";

	public override void _Ready()
	{
		GD.Print("=== CyberPond Starting ===");

		var config = LoadConfig(ConfigPath);
		if (config == null)
		{
			GD.PrintErr("[GameManager] Failed to load game config, aborting.");
			return;
		}

		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		var saveManager = GetNode<SaveManager>("/root/SaveManager");
		var pondManager = GetNode<PondManager>("/root/PondManager");
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");

		var saveData = saveManager.LoadGame();
		if (saveData != null)
		{
			var playerDict = (Dictionary)saveData["player"];
			economy.SetCoins(playerDict["coins"].AsInt32());

			var pondsArray = (Array)saveData["ponds"];
			foreach (var pondDict in pondsArray)
				pondManager.RestorePond((Dictionary)pondDict);

			inventory.FromDict((Dictionary)saveData["inventory"]);

			GD.Print($"[GameManager] Save restored. Coins: {economy.Coins}, Ponds: {pondManager.PondCount}");
		}
		else
		{
			var startingCoins = config["starting_coins"].AsInt32();
			economy.SetCoins(startingCoins);
			saveManager.SaveGame();
			GD.Print($"[GameManager] Fresh start. Coins: {economy.Coins}");
		}

		GD.Print("=== CyberPond Ready ===");
	}

	private Dictionary LoadConfig(string path)
	{
		if (!FileAccess.FileExists(path))
		{
			GD.PrintErr($"[GameManager] Config file not found: {path}");
			return null;
		}
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		var error = json.Parse(file.GetAsText());
		if (error != Error.Ok)
		{
			GD.PrintErr($"[GameManager] Failed to parse config: {error}");
			return null;
		}
		return json.Data.AsGodotDictionary();
	}
}
