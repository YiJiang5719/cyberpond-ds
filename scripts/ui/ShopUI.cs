using Godot;
using Godot.Collections;

namespace CyberPond;

public partial class ShopUI : Control
{
	private VBoxContainer _fishList;
	private VBoxContainer _ticketList;
	private Label _coinsLabel;
	private Dictionary _fishTypes;
	private Dictionary _shopConfig;
	private Array _availableTickets;
	private float _buyPriceMultiplier;
	private float _sellPriceMultiplier;

	// Detail popup nodes
	private ColorRect _popupOverlay;
	private Label _popupTitle;
	private Label _popupDesc;
	private Label _popupGrowth;
	private Label _popupSellPrice;
	private Label _popupBuyPrice;
	private Button _popupBuyBtn;
	private TextureRect _popupSprite;
	private string _selectedFishType;
	private int _selectedFishPrice;

	public override void _Ready()
	{
		_coinsLabel = GetNode<Label>("CoinsLabel");
		_fishList = GetNode<VBoxContainer>("FishScroll/FishList");
		_ticketList = GetNode<VBoxContainer>("TicketScroll/TicketList");

		// Bottom nav
		GetNode<Button>("BottomNav/NavButtons/PondBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");
		GetNode<Button>("BottomNav/NavButtons/DiscoverBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/discover.tscn");
		GetNode<Button>("BottomNav/NavButtons/InventoryBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/inventory.tscn");

		_fishTypes = LoadConfig("res://configs/fish_types.json");
		_shopConfig = LoadConfig("res://configs/shop_config.json");
		_buyPriceMultiplier = _shopConfig["buy_price_multiplier"].AsSingle();
		_sellPriceMultiplier = _shopConfig["sell_price_multiplier"].AsSingle();
		_availableTickets = (Array)_shopConfig["available_tickets"];

		CreatePopup();
		Refresh();
	}

	public void Refresh()
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		_coinsLabel.Text = $"Coins: {economy.Coins}";

		foreach (var child in _fishList.GetChildren())
			child.QueueFree();

		GD.Print($"[ShopUI] Loading {_fishTypes.Keys.Count} fish types...");
		foreach (var key in _fishTypes.Keys)
		{
			var fishDef = (Dictionary)_fishTypes[key.ToString()];
			GD.Print($"[ShopUI] Creating card for: {fishDef["name"]}");
			_fishList.AddChild(CreateFishCard(fishDef));
		}
		foreach (var child in _ticketList.GetChildren())
			child.QueueFree();

		foreach (Dictionary ticket in _availableTickets)
		{
			_ticketList.AddChild(CreateTicketCard(ticket));
		}

		GD.Print($"[ShopUI] Done. Fish: {_fishList.GetChildCount()}, Tickets: {_ticketList.GetChildCount()}");
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

	private Control CreateFishCard(Dictionary fishDef)
	{
		var card = new Button();
		card.SizeFlagsHorizontal = Control.SizeFlags.Fill;
		card.CustomMinimumSize = new Vector2(0, 72);
		card.Flat = true;
		card.AddThemeStyleboxOverride("normal", new StyleBoxFlat
		{
			BgColor = new Color("#FFFFFF"),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 16,
			ContentMarginRight = 16,
			ContentMarginTop = 12,
			ContentMarginBottom = 12
		});
		card.AddThemeStyleboxOverride("hover", new StyleBoxFlat
		{
			BgColor = new Color("#E3F2FD"),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 16,
			ContentMarginRight = 16,
			ContentMarginTop = 12,
			ContentMarginBottom = 12
		});

		var hbox = new HBoxContainer();
		hbox.MouseFilter = MouseFilterEnum.Ignore;
		hbox.AddThemeConstantOverride("separation", 12);

		var sprite = CreateFishSprite(fishDef["id"].ToString());
		hbox.AddChild(sprite);

		var info = new VBoxContainer();
		info.MouseFilter = MouseFilterEnum.Ignore;
		var nameLabel = new Label { Text = fishDef["name"].ToString() };
		nameLabel.AddThemeFontSizeOverride("font_size", 18);

		var descLabel = new Label { Text = fishDef["description"].ToString() };
		descLabel.AddThemeColorOverride("font_color", new Color("#757575"));
		descLabel.AddThemeFontSizeOverride("font_size", 12);

		int price = Mathf.RoundToInt(fishDef["price"].AsInt32() * _buyPriceMultiplier);
		var priceLabel = new Label { Text = $"Price: {price} coins" };
		priceLabel.AddThemeColorOverride("font_color", new Color("#FFC107"));

		info.AddChild(nameLabel);
		info.AddChild(descLabel);
		info.AddChild(priceLabel);

		hbox.AddChild(info);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand, MouseFilter = MouseFilterEnum.Ignore });

		var arrow = new Label { Text = ">" };
		arrow.AddThemeColorOverride("font_color", new Color("#90CAF9"));
		hbox.AddChild(arrow);

		card.AddChild(hbox);

		var fishType = fishDef["id"].ToString();
		card.Pressed += () => ShowPopup(fishType, price);

		return card;
	}

	private Control CreateTicketCard(Dictionary ticketDef)
	{
		var card = new Button();
		card.SizeFlagsHorizontal = Control.SizeFlags.Fill;
		card.CustomMinimumSize = new Vector2(0, 64);
		card.Flat = true;
		card.AddThemeStyleboxOverride("normal", new StyleBoxFlat
		{
			BgColor = new Color("#FFFFFF"),
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomLeft = 8,
			CornerRadiusBottomRight = 8,
			ContentMarginLeft = 16,
			ContentMarginRight = 16,
			ContentMarginTop = 10,
			ContentMarginBottom = 10
		});

		var hbox = new HBoxContainer();
		hbox.MouseFilter = MouseFilterEnum.Ignore;
		hbox.AddThemeConstantOverride("separation", 12);

		var info = new VBoxContainer();
		info.MouseFilter = MouseFilterEnum.Ignore;

		var nameLabel = new Label { Text = ticketDef["name"].AsString() };
		nameLabel.AddThemeFontSizeOverride("font_size", 16);

		int price = ticketDef["price"].AsInt32();
		var priceLabel = new Label { Text = price == 0 ? "Free" : $"Price: {price} coins" };
		priceLabel.AddThemeColorOverride("font_color", new Color(price == 0 ? "#66BB6A" : "#FFC107"));

		info.AddChild(nameLabel);
		info.AddChild(priceLabel);

		hbox.AddChild(info);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand, MouseFilter = MouseFilterEnum.Ignore });

		var buyBtn = new Button { Text = price == 0 ? "Get" : "Buy" };
		buyBtn.CustomMinimumSize = new Vector2(70, 36);
		buyBtn.AddThemeFontSizeOverride("font_size", 14);

		var ticketId = ticketDef["id"].AsString();
		buyBtn.Pressed += () => OnBuyTicket(ticketId, price);
		hbox.AddChild(buyBtn);

		card.AddChild(hbox);
		return card;
	}

	private void OnBuyTicket(string ticketId, int price)
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");

		if (!economy.SpendCoins(price))
		{
			GD.Print("[ShopUI] Not enough coins for ticket.");
			return;
		}

		inventory.AddItem(ticketId);
		GetNode<SaveManager>("/root/SaveManager").SaveGame();
		Refresh();
		GD.Print($"[ShopUI] Purchased ticket: {ticketId}");
	}

	private void CreatePopup()
	{
		_popupOverlay = new ColorRect();
		_popupOverlay.MouseFilter = MouseFilterEnum.Stop;
		_popupOverlay.SetAnchorsPreset(LayoutPreset.FullRect);
		_popupOverlay.Color = new Color(0, 0, 0, 0.4f);
		_popupOverlay.Hide();
		_popupOverlay.GuiInput += (e) =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed)
				_popupOverlay.Hide();
		};

		var panel = new PanelContainer();
		panel.SetAnchorsPreset(LayoutPreset.Center);
		panel.CustomMinimumSize = new Vector2(320, 0);
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color("#FFFFFF"),
			CornerRadiusTopLeft = 12,
			CornerRadiusTopRight = 12,
			CornerRadiusBottomLeft = 12,
			CornerRadiusBottomRight = 12,
			ContentMarginLeft = 24,
			ContentMarginRight = 24,
			ContentMarginTop = 20,
			ContentMarginBottom = 20
		});

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);

		_popupTitle = new Label();
		_popupTitle.AddThemeFontSizeOverride("font_size", 20);

		_popupSprite = new TextureRect();
		_popupSprite.CustomMinimumSize = new Vector2(80, 80);
		_popupSprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		_popupSprite.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;

		_popupDesc = new Label();
		_popupDesc.AddThemeColorOverride("font_color", new Color("#757575"));
		_popupDesc.AddThemeFontSizeOverride("font_size", 13);
		_popupDesc.AutowrapMode = TextServer.AutowrapMode.WordSmart;

		_popupGrowth = new Label();
		_popupGrowth.AddThemeFontSizeOverride("font_size", 14);

		_popupSellPrice = new Label();
		_popupSellPrice.AddThemeFontSizeOverride("font_size", 14);
		_popupSellPrice.AddThemeColorOverride("font_color", new Color("#66BB6A"));

		_popupBuyPrice = new Label();
		_popupBuyPrice.AddThemeFontSizeOverride("font_size", 16);
		_popupBuyPrice.AddThemeColorOverride("font_color", new Color("#FFC107"));

		var btnRow = new HBoxContainer();
		btnRow.AddThemeConstantOverride("separation", 12);

		_popupBuyBtn = new Button { Text = "Buy" };
		_popupBuyBtn.CustomMinimumSize = new Vector2(100, 42);
		_popupBuyBtn.AddThemeFontSizeOverride("font_size", 15);
		_popupBuyBtn.Pressed += OnBuyFish;

		var closeBtn = new Button { Text = "Close" };
		closeBtn.CustomMinimumSize = new Vector2(100, 42);
		closeBtn.AddThemeFontSizeOverride("font_size", 15);
		closeBtn.Pressed += () => _popupOverlay.Hide();

		btnRow.AddChild(_popupBuyBtn);
		btnRow.AddChild(closeBtn);

		vbox.AddChild(_popupTitle);
		vbox.AddChild(_popupSprite);
		vbox.AddChild(_popupDesc);
		vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
		vbox.AddChild(_popupGrowth);
		vbox.AddChild(_popupSellPrice);
		vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
		vbox.AddChild(_popupBuyPrice);
		vbox.AddChild(btnRow);

		panel.AddChild(vbox);
		_popupOverlay.AddChild(panel);
		AddChild(_popupOverlay);
	}

	private void ShowPopup(string fishType, int price)
	{
		_selectedFishType = fishType;
		_selectedFishPrice = price;
		var fishDef = (Dictionary)_fishTypes[fishType];

		_popupTitle.Text = fishDef["name"].ToString();
		_popupSprite.Texture = GD.Load<Texture2D>(fishDef["sprite"].ToString());
		_popupDesc.Text = fishDef["description"].ToString();

		int hours = fishDef["growth_time_hours"].AsInt32();
		_popupGrowth.Text = hours < 24
			? $"Grow time: {hours} hours"
			: $"Grow time: {hours / 24} days {(hours % 24 > 0 ? $"{hours % 24}h" : "")}";

		int roePrice = Mathf.RoundToInt(fishDef["roe_price"].AsInt32() * _sellPriceMultiplier);
		int dailyRoe = fishDef["daily_roe"].AsInt32();
		_popupSellPrice.Text = $"Roe: {dailyRoe}/day, sell at {roePrice} coin(s) each";

		_popupBuyPrice.Text = $"Buy price: {price} coins";

		_popupOverlay.Show();
	}

	private void OnBuyFish()
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");

		if (!economy.SpendCoins(_selectedFishPrice))
		{
			GD.Print("[ShopUI] Not enough coins.");
			return;
		}

		if (inventory.GetFryCount(_selectedFishType) == 0 && !inventory.CanAddItem())
		{
			economy.AddCoins(_selectedFishPrice);
			GD.Print("[ShopUI] Inventory full.");
			return;
		}

		inventory.AddFry(_selectedFishType);
		GetNode<SaveManager>("/root/SaveManager").SaveGame();
		_popupOverlay.Hide();
		Refresh();
	}

	private Dictionary LoadConfig(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		json.Parse(file.GetAsText());
		return json.Data.AsGodotDictionary();
	}
}
