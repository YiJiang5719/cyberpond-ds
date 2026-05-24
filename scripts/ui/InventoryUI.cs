using Godot;

namespace CyberPond;

public partial class InventoryUI : Control
{
	private VBoxContainer _fryList;
	private VBoxContainer _roeList;
	private VBoxContainer _itemsList;
	private Label _coinsLabel;
	private Label _slotLabel;

	public override void _Ready()
	{
		_coinsLabel = GetNode<Label>("CoinsLabel");
		_slotLabel = GetNode<Label>("SlotLabel");
		_fryList = GetNode<VBoxContainer>("FryScroll/FryList");
		_roeList = GetNode<VBoxContainer>("RoeScroll/RoeList");
		_itemsList = GetNode<VBoxContainer>("ItemsScroll/ItemsList");

		GetNode<Button>("BottomNav/NavButtons/PondBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");
		GetNode<Button>("BottomNav/NavButtons/DiscoverBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/discover.tscn");
		GetNode<Button>("BottomNav/NavButtons/ShopBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/shop.tscn");

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
		foreach (var child in _itemsList.GetChildren())
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

		foreach (var kv in inventory.GetAllItems())
		{
			if (kv.Value <= 0) continue;
			_itemsList.AddChild(CreateItemEntry(kv.Key, kv.Value));
		}
	}

	private TextureRect CreateFishSprite(string fishType)
	{
		var rect = new TextureRect();
		rect.CustomMinimumSize = new Vector2(48, 48);
		rect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		rect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		var fishManager = GetNode<FishManager>("/root/FishManager");
		var path = fishManager.GetSpritePath(fishType);
		if (!string.IsNullOrEmpty(path))
			rect.Texture = GD.Load<Texture2D>(path);
		return rect;
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

		var sprite = CreateFishSprite(fishType);
		hbox.AddChild(sprite);

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

		var sprite = CreateFishSprite(fishType);
		hbox.AddChild(sprite);

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

	private Control CreateItemEntry(string itemId, int count)
	{
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color("#FFFFFF"),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 10,
			ContentMarginBottom = 10
		});

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 12);

		// Ticket sprite
		string spritePath = GetItemSprite(itemId);
		if (!string.IsNullOrEmpty(spritePath))
		{
			var tex = GD.Load<Texture2D>(spritePath);
			if (tex != null)
			{
				var spriteRect = new TextureRect();
				spriteRect.Texture = tex;
				spriteRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
				spriteRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
				spriteRect.CustomMinimumSize = new Vector2(48, 48);
				hbox.AddChild(spriteRect);
			}
		}

		var info = new VBoxContainer();
		var nameLabel = new Label { Text = GetItemDisplayName(itemId) };
		nameLabel.AddThemeFontSizeOverride("font_size", 26);

		var descLabel = new Label { Text = GetItemDescription(itemId) };
		descLabel.AddThemeColorOverride("font_color", new Color("#777777"));
		descLabel.AddThemeFontSizeOverride("font_size", 20);

		var countLabel = new Label { Text = $"数量: {count}" };
		countLabel.AddThemeColorOverride("font_color", new Color("#42A5F5"));
		countLabel.AddThemeFontSizeOverride("font_size", 22);

		info.AddChild(nameLabel);
		info.AddChild(descLabel);
		info.AddChild(countLabel);

		hbox.AddChild(info);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand });

		panel.AddChild(hbox);
		return panel;
	}

	private static string GetItemSprite(string itemId)
	{
		// Check islands.json for ticket_sprite
		var config = LoadConfigStatic("res://configs/islands.json");
		var islands = (Godot.Collections.Array)config["islands"];
		foreach (Godot.Collections.Dictionary island in islands)
		{
			if (island["ticket_id"].AsString() == itemId && island.TryGetValue("ticket_sprite", out var sp))
				return sp.AsString();
		}
		// Check shop_config.json tickets
		var shopConfig = LoadConfigStatic("res://configs/shop_config.json");
		var tickets = (Godot.Collections.Array)shopConfig["available_tickets"];
		foreach (Godot.Collections.Dictionary ticket in tickets)
		{
			if (ticket["id"].AsString() == itemId && ticket.TryGetValue("sprite", out var sp))
				return sp.AsString();
		}
		return "";
	}

	private static string GetItemDescription(string itemId)
	{
		// Check shop_config.json tickets
		var shopConfig = LoadConfigStatic("res://configs/shop_config.json");
		var tickets = (Godot.Collections.Array)shopConfig["available_tickets"];
		foreach (Godot.Collections.Dictionary ticket in tickets)
		{
			if (ticket["id"].AsString() == itemId && ticket.TryGetValue("description", out var desc))
				return desc.AsString();
		}
		return "";
	}

	private static string GetItemDisplayName(string itemId)
	{
		// Try to resolve from islands.json ticket_id
		var config = LoadConfigStatic("res://configs/islands.json");
		var islands = (Godot.Collections.Array)config["islands"];
		foreach (Godot.Collections.Dictionary island in islands)
		{
			if (island["ticket_id"].AsString() == itemId)
				return $"船票 — {island["name"]}";
		}
		// Fallback for shop-config names
		var shopConfig = LoadConfigStatic("res://configs/shop_config.json");
		var tickets = (Godot.Collections.Array)shopConfig["available_tickets"];
		foreach (Godot.Collections.Dictionary ticket in tickets)
		{
			if (ticket["id"].AsString() == itemId)
				return ticket["name"].AsString();
		}
		return itemId;
	}

	private static Godot.Collections.Dictionary LoadConfigStatic(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		json.Parse(file.GetAsText());
		return json.Data.AsGodotDictionary();
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
