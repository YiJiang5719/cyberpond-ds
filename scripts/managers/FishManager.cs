using System;
using Godot;
using Godot.Collections;

namespace CyberPond;

/// Manages fish lifecycle: fry placement, time-based growth, daily roe production.
public partial class FishManager : Node
{
	private const string FishTypesPath = "res://configs/fish_types.json";
	private Dictionary _fishConfig;

	public override void _Ready()
	{
		_fishConfig = LoadConfig(FishTypesPath);
		GD.Print("[FishManager] Ready.");
	}

	public bool PlaceFry(string pondId, string fishType)
	{
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");
		if (inventory.GetFryCount(fishType) <= 0)
			return false;

		var pondManager = GetNode<PondManager>("/root/PondManager");
		var pond = pondManager.GetPond(pondId);
		if (pond == null) return false;

		int index = pondManager.GetAllPonds().IndexOf(pond);
		int maxFish = pondManager.GetMaxFishCount(index);
		if (pond.Fishes.Count >= maxFish) return false;

		if (!inventory.RemoveFry(fishType)) return false;

		var fish = new FishData(fishType);
		pond.Fishes.Add(fish);
		GetNode<SaveManager>("/root/SaveManager").SaveGame();
		GD.Print($"[FishManager] Placed {fishType} fry into pond {pondId}");
		return true;
	}

	public void UpdateFishStates()
	{
		var pondManager = GetNode<PondManager>("/root/PondManager");
		var now = DateTime.UtcNow;
		bool changed = false;

		foreach (var pond in pondManager.GetAllPonds())
		{
			foreach (var fish in pond.Fishes)
			{
				var fishDef = (Dictionary)_fishConfig[fish.FishType];
				int growthHours = fishDef["growth_time_hours"].AsInt32();
				int dailyRoe = fishDef["daily_roe"].AsInt32();

				if (!fish.IsAdult && now >= fish.SpawnedAt.AddHours(growthHours))
				{
					fish.IsAdult = true;
					changed = true;
				}

				if (fish.IsAdult)
				{
					var elapsed = now - fish.LastRoeTime;
					if (elapsed.TotalHours >= 24)
					{
						int days = (int)(elapsed.TotalHours / 24);
						fish.UncollectedRoe += dailyRoe * days;
						fish.LastRoeTime = fish.LastRoeTime.AddHours(24 * days);
						changed = true;
					}
				}
			}
		}

		if (changed)
			GetNode<SaveManager>("/root/SaveManager").SaveGame();
	}

	public int GetUncollectedRoe(PondData pond)
	{
		int total = 0;
		foreach (var fish in pond.Fishes)
			total += fish.UncollectedRoe;
		return total;
	}

	public int CollectRoe(string pondId)
	{
		var pondManager = GetNode<PondManager>("/root/PondManager");
		var pond = pondManager.GetPond(pondId);
		if (pond == null) return 0;

		var inventory = GetNode<InventoryManager>("/root/InventoryManager");
		int total = 0;
		foreach (var fish in pond.Fishes)
		{
			if (fish.UncollectedRoe > 0)
			{
				inventory.AddRoe(fish.FishType, fish.UncollectedRoe);
				total += fish.UncollectedRoe;
				fish.UncollectedRoe = 0;
			}
		}

		if (total > 0)
		{
			GetNode<SaveManager>("/root/SaveManager").SaveGame();
			GD.Print($"[FishManager] Collected {total} roe from pond {pondId}");
		}

		return total;
	}

	public string GetGrowthRemaining(FishData fish)
	{
		if (fish.IsAdult) return "Adult";

		var fishDef = (Dictionary)_fishConfig[fish.FishType];
		int growthHours = fishDef["growth_time_hours"].AsInt32();
		var grownAt = fish.SpawnedAt.AddHours(growthHours);
		var remaining = grownAt - DateTime.UtcNow;

		if (remaining.TotalHours <= 0) return "Adult";

		if (remaining.TotalHours >= 24)
			return $"Growing ({remaining.Days}d {remaining.Hours}h)";
		if (remaining.TotalHours >= 1)
			return $"Growing ({remaining.Hours}h {remaining.Minutes}m)";
		return $"Growing ({remaining.Minutes}m)";
	}

	public string GetDisplayName(string typeKey) => typeKey switch
	{
		"common_carp" => "Common Carp",
		"goldfish" => "Goldfish",
		"koi" => "Koi",
		"arowana" => "Arowana",
		_ => typeKey
	};

	private Dictionary LoadConfig(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		json.Parse(file.GetAsText());
		return json.Data.AsGodotDictionary();
	}
}
