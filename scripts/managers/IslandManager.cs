using Godot;
using Godot.Collections;

namespace CyberPond;

/// Manages island discovery, visit state, and ticket consumption.
public partial class IslandManager : Node
{
	private const string IslandsConfigPath = "res://configs/islands.json";

	private Dictionary _islandsConfig;
	private string _currentIslandId;

	public string CurrentIslandId
	{
		get => _currentIslandId;
		set
		{
			_currentIslandId = value;
			GD.Print($"[IslandManager] CurrentIslandId set to: {_currentIslandId ?? "null"}");
		}
	}

	public bool IsOnIsland => !string.IsNullOrEmpty(_currentIslandId);

	public override void _Ready()
	{
		_islandsConfig = LoadConfig(IslandsConfigPath);
		GD.Print("[IslandManager] Ready.");
	}

	public Dictionary GetIslandConfig(string islandId)
	{
		var islands = (Array)_islandsConfig["islands"];
		foreach (Dictionary island in islands)
		{
			if (island["id"].AsString() == islandId)
				return island;
		}
		return null;
	}

	public Array GetAllIslands() => (Array)_islandsConfig["islands"];

	public bool ConsumeTicket(string islandId)
	{
		var islandConfig = GetIslandConfig(islandId);
		if (islandConfig == null) return false;

		string ticketId = islandConfig["ticket_id"].AsString();
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");
		if (inventory.GetItemCount(ticketId) <= 0) return false;

		inventory.RemoveItem(ticketId, 1);
		GD.Print($"[IslandManager] Ticket consumed: {ticketId}");
		return true;
	}

	public int GetTicketCount(string islandId)
	{
		var islandConfig = GetIslandConfig(islandId);
		if (islandConfig == null) return 0;

		string ticketId = islandConfig["ticket_id"].AsString();
		return GetNode<InventoryManager>("/root/InventoryManager").GetItemCount(ticketId);
	}

	public void LeaveIsland()
	{
		GD.Print($"[IslandManager] Leaving island: {_currentIslandId}");
		_currentIslandId = null;
	}

	private Dictionary LoadConfig(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		json.Parse(file.GetAsText());
		return json.Data.AsGodotDictionary();
	}
}
