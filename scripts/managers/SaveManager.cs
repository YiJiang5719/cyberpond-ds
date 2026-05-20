using Godot;
using Godot.Collections;

namespace CyberPond;

/// Handles reading and writing game save data to local JSON file.
public partial class SaveManager : Node
{
	private const string SavePath = "user://save_data.json";

	public Dictionary GetSaveData()
	{
		var pondsArray = new Array();
		var pondManager = GetNode<PondManager>("/root/PondManager");
		foreach (var pond in pondManager.GetAllPonds())
			pondsArray.Add(pond.ToDict());

		var data = new Dictionary
		{
			{ "player", new Dictionary { { "coins", GetNode<EconomyManager>("/root/EconomyManager").Coins } } },
			{ "ponds", pondsArray },
			{ "inventory", new Dictionary
				{
					{ "unlocked_slots", 10 },
					{ "fry", new Dictionary() },
					{ "roe", new Dictionary() }
				}
			}
		};
		return data;
	}

	public void SaveGame()
	{
		var data = GetSaveData();
		var jsonString = Json.Stringify(data, "\t");
		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		if (file != null)
		{
			file.StoreString(jsonString);
			GD.Print($"[SaveManager] Game saved. Coins: {((Dictionary)data["player"])["coins"]}");
		}
	}

	public Dictionary LoadGame()
	{
		if (!FileAccess.FileExists(SavePath))
		{
			GD.Print("[SaveManager] No save file found, fresh start.");
			return null;
		}

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		var jsonString = file.GetAsText();
		var json = new Json();
		var error = json.Parse(jsonString);
		if (error != Error.Ok)
		{
			GD.PrintErr($"[SaveManager] Failed to parse save: {error}");
			return null;
		}

		GD.Print("[SaveManager] Save file loaded.");
		return json.Data.AsGodotDictionary();
	}
}
