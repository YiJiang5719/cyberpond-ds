using System.Collections.Generic;
using Godot;

namespace CyberPond;

/// Manages inventory state: fry, roe, and slot unlocks.
public partial class InventoryManager : Node
{
	private const string ConfigPath = "res://configs/inventory_config.json";

	private Dictionary<string, int> _fry = new();
	private Dictionary<string, int> _roe = new();
	private int _unlockedSlots = 10;
	private int _maxSlots = 30;
	private int _slotUnlockCost = 100;

	public int UnlockedSlots => _unlockedSlots;
	public int MaxSlots => _maxSlots;
	public int SlotUnlockCost => _slotUnlockCost;

	public override void _Ready()
	{
		var config = LoadConfig();
		_unlockedSlots = config["initial_slots"].AsInt32();
		_maxSlots = config["max_slots"].AsInt32();
		_slotUnlockCost = config["slot_unlock_cost"].AsInt32();
		GD.Print($"[InventoryManager] Ready. Slots: {_unlockedSlots}/{_maxSlots}");
	}

	public int GetFryCount(string fishType) =>
		_fry.TryGetValue(fishType, out var count) ? count : 0;

	public int GetRoeCount(string fishType) =>
		_roe.TryGetValue(fishType, out var count) ? count : 0;

	public Dictionary<string, int> GetAllFry() => new(_fry);
	public Dictionary<string, int> GetAllRoe() => new(_roe);

	public int TotalItemTypes()
	{
		int count = 0;
		foreach (var v in _fry.Values)
			if (v > 0) count++;
		foreach (var v in _roe.Values)
			if (v > 0) count++;
		return count;
	}

	public bool CanAddItem()
	{
		return TotalItemTypes() < _unlockedSlots;
	}

	public void AddFry(string fishType, int count = 1)
	{
		_fry[fishType] = GetFryCount(fishType) + count;
		GD.Print($"[InventoryManager] Added {count} {fishType} fry. Total: {_fry[fishType]}");
	}

	public bool RemoveFry(string fishType, int count = 1)
	{
		int current = GetFryCount(fishType);
		if (current < count) return false;
		_fry[fishType] = current - count;
		return true;
	}

	public void AddRoe(string fishType, int count = 1)
	{
		_roe[fishType] = GetRoeCount(fishType) + count;
	}

	public bool RemoveRoe(string fishType, int count)
	{
		int current = GetRoeCount(fishType);
		if (current < count) return false;
		_roe[fishType] = current - count;
		return true;
	}

	public bool UnlockSlot()
	{
		if (_unlockedSlots >= _maxSlots)
			return false;
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		if (!economy.SpendCoins(_slotUnlockCost))
			return false;
		_unlockedSlots++;
		GD.Print($"[InventoryManager] Slot unlocked. Now: {_unlockedSlots}/{_maxSlots}");
		return true;
	}

	public Godot.Collections.Dictionary ToDict()
	{
		var fryDict = new Godot.Collections.Dictionary();
		foreach (var kv in _fry)
			fryDict[kv.Key] = kv.Value;

		var roeDict = new Godot.Collections.Dictionary();
		foreach (var kv in _roe)
			roeDict[kv.Key] = kv.Value;

		return new Godot.Collections.Dictionary
		{
			{ "unlocked_slots", _unlockedSlots },
			{ "fry", fryDict },
			{ "roe", roeDict }
		};
	}

	public void FromDict(Godot.Collections.Dictionary dict)
	{
		_unlockedSlots = dict["unlocked_slots"].AsInt32();
		_fry.Clear();
		_roe.Clear();

		var fryDict = (Godot.Collections.Dictionary)dict["fry"];
		foreach (var key in fryDict.Keys)
			_fry[key.ToString()] = fryDict[key].AsInt32();

		var roeDict = (Godot.Collections.Dictionary)dict["roe"];
		foreach (var key in roeDict.Keys)
			_roe[key.ToString()] = roeDict[key].AsInt32();

		GD.Print($"[InventoryManager] Restored. Slots: {_unlockedSlots}, Fry types: {_fry.Count}, Roe types: {_roe.Count}");
	}

	private Godot.Collections.Dictionary LoadConfig()
	{
		using var file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
		var json = new Json();
		json.Parse(file.GetAsText());
		return json.Data.AsGodotDictionary();
	}
}
