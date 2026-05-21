using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace CyberPond;

/// Manages all pond lifecycle: create, query, unlock cost calculation.
public partial class PondManager : Node
{
    private const string PondConfigPath = "res://configs/pond_config.json";
    private const string GameConfigPath = "res://configs/game_config.json";

    private List<PondData> _ponds = new();
    private Dictionary _pondConfig;
    private Dictionary _gameConfig;

    public int PondCount => _ponds.Count;
    public string SelectedPondId { get; set; }

    public override void _Ready()
    {
        _pondConfig = LoadConfig(PondConfigPath);
        _gameConfig = LoadConfig(GameConfigPath);
        GD.Print($"[PondManager] Configs loaded. Max ponds: {_gameConfig["max_pond_count"]}");
    }

    public List<PondData> GetAllPonds() => _ponds;

    public PondData GetPond(string id) => _ponds.Find(p => p.Id == id);

    public int GetUnlockCost()
    {
        int index = _ponds.Count; // Next pond is index+1, but we use 0-based array
        var costs = (Array)_pondConfig["ponds"];
        if (index < costs.Count)
            return ((Dictionary)costs[index])["unlock_cost"].AsInt32();
        return -1; // Already max
    }

    public int GetMaxFishForNextPond()
    {
        int index = _ponds.Count;
        var costs = (Array)_pondConfig["ponds"];
        if (index < costs.Count)
            return ((Dictionary)costs[index])["max_fish_count"].AsInt32();
        return -1;
    }

    public bool CanCreateMorePonds()
    {
        int maxPonds = _gameConfig["max_pond_count"].AsInt32();
        return _ponds.Count < maxPonds;
    }

    public int GetMaxFishCount(int pondIndex)
    {
        var costs = (Array)_pondConfig["ponds"];
        if (pondIndex < _ponds.Count && pondIndex < costs.Count)
            return ((Dictionary)costs[pondIndex])["max_fish_count"].AsInt32();
        if (pondIndex < costs.Count)
            return ((Dictionary)costs[pondIndex])["max_fish_count"].AsInt32();
        return -1;
    }

    public bool CreatePond(string name, double lat, double lon)
    {
        if (!CanCreateMorePonds())
        {
            GD.Print("[PondManager] Max pond count reached.");
            return false;
        }

        int cost = GetUnlockCost();
        if (cost > 0)
        {
            var economy = GetNode<EconomyManager>("/root/EconomyManager");
            if (!economy.SpendCoins(cost))
            {
                GD.Print($"[PondManager] Cannot afford unlock cost: {cost}");
                return false;
            }
        }

        var pond = new PondData(name, lat, lon);
        _ponds.Add(pond);
        GD.Print($"[PondManager] Pond created: {name} (id: {pond.Id})");
        return true;
    }

    public void RestorePond(Dictionary dict)
    {
        _ponds.Add(PondData.FromDict(dict));
    }

    public void ResetAll()
    {
        _ponds.Clear();
        GD.Print("[PondManager] All ponds cleared.");
    }

    private Dictionary LoadConfig(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var json = new Json();
        json.Parse(file.GetAsText());
        return json.Data.AsGodotDictionary();
    }
}
