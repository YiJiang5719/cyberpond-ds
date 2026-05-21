using Godot;

namespace CyberPond;

public partial class InventoryUI : Control
{
	private VBoxContainer _fryList;
	private VBoxContainer _roeList;
	private Label _coinsLabel;
	private Label _slotLabel;
	private Button _mapBtn;
	private Button _shopBtn;
	private Button _inventoryBtn;

	public override void _Ready()
	{
		_coinsLabel = GetNode<Label>("CoinsLabel");
		_slotLabel = GetNode<Label>("SlotLabel");
		_fryList = GetNode<VBoxContainer>("FryScroll/FryList");
		_roeList = GetNode<VBoxContainer>("RoeScroll/RoeList");

		_mapBtn = GetNode<Button>("BottomNav/NavButtons/MapBtn");
		_shopBtn = GetNode<Button>("BottomNav/NavButtons/ShopBtn");
		_inventoryBtn = GetNode<Button>("BottomNav/NavButtons/InventoryBtn");

		_mapBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");
		_shopBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/shop.tscn");

		GetNode<Button>("UnlockSlotBtn").Pressed += OnUnlockSlot;

		Refresh();
	}

	public void Refresh()
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		_coinsLabel.Text = $"Coins: {economy.Coins}";

		var inventory = GetNode<InventoryManager>("/root/InventoryManager");
		_slotLabel.Text = $"Slots: {inventory.TotalItemTypes()} / {inventory.UnlockedSlots}";

		foreach (var child in _fryList.GetChildren())
			child.QueueFree();
		foreach (var child in _roeList.GetChildren())
			child.QueueFree();

		foreach (var kv in inventory.GetAllFry())
		{
			if (kv.Value <= 0) continue;
			_fryList.AddChild(CreateFryEntry(kv.Key, kv.Value));
		}

		foreach (var kv in inventory.GetAllRoe())
		{
			if (kv.Value <= 0) continue;
			_roeList.AddChild(CreateRoeEntry(kv.Key, kv.Value));
		}
	}

	private Control CreateFryEntry(string fishType, int count)
	{
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color("#FFFFFF"),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		});

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 8);

		var nameLabel = new Label { Text = $"{GetDisplayName(fishType)} Fry x{count}" };
		hbox.AddChild(nameLabel);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand });

		var statusLabel = new Label { Text = "Can place in pond" };
		statusLabel.AddThemeColorOverride("font_color", new Color("#42A5F5"));
		hbox.AddChild(statusLabel);

		panel.AddChild(hbox);
		return panel;
	}

	private Control CreateRoeEntry(string fishType, int count)
	{
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color("#FFFFFF"),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 8,
			ContentMarginBottom = 8
		});

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 8);

		var nameLabel = new Label { Text = $"{GetDisplayName(fishType)} Roe x{count}" };
		hbox.AddChild(nameLabel);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand });

		var sellBtn = new Button { Text = "Sell" };
		sellBtn.CustomMinimumSize = new Vector2(60, 32);
		sellBtn.Pressed += () => OnSellRoe(fishType);
		hbox.AddChild(sellBtn);

		panel.AddChild(hbox);
		return panel;
	}

	private void OnSellRoe(string fishType)
	{
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");
		int count = inventory.GetRoeCount(fishType);
		if (count <= 0) return;

		var fishConfig = LoadConfig("res://configs/fish_types.json");
		var fishDef = (Godot.Collections.Dictionary)fishConfig[fishType];
		var shopConfig = LoadConfig("res://configs/shop_config.json");
		float multiplier = shopConfig["sell_price_multiplier"].AsSingle();
		int unitPrice = Mathf.RoundToInt(fishDef["roe_price"].AsInt32() * multiplier);

		inventory.RemoveRoe(fishType, count);
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		economy.AddCoins(unitPrice * count);
		GetNode<SaveManager>("/root/SaveManager").SaveGame();
		Refresh();

		GD.Print($"[InventoryUI] Sold {count} {fishType} roe for {unitPrice * count} coins.");
	}

	private void OnUnlockSlot()
	{
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");
		if (inventory.UnlockedSlots >= inventory.MaxSlots)
		{
			GD.Print("[InventoryUI] Max slots reached.");
			return;
		}
		if (inventory.UnlockSlot())
		{
			GetNode<SaveManager>("/root/SaveManager").SaveGame();
			Refresh();
		}
	}

	private static string GetDisplayName(string typeKey) => typeKey switch
	{
		"common_carp" => "Common Carp",
		"goldfish" => "Goldfish",
		"koi" => "Koi",
		"arowana" => "Arowana",
		_ => typeKey
	};

	private Godot.Collections.Dictionary LoadConfig(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		json.Parse(file.GetAsText());
		return json.Data.AsGodotDictionary();
	}
}
