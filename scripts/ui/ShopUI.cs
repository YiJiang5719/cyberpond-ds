using Godot;
using Godot.Collections;

namespace CyberPond;

public partial class ShopUI : Control
{
	private VBoxContainer _fishList;
	private Label _coinsLabel;
	private Button _mapBtn;
	private Button _shopBtn;
	private Button _inventoryBtn;
	private Dictionary _fishTypes;
	private float _buyPriceMultiplier;

	public override void _Ready()
	{
		_coinsLabel = GetNode<Label>("CoinsLabel");
		_fishList = GetNode<VBoxContainer>("FishScroll/FishList");

		_mapBtn = GetNode<Button>("BottomNav/MapBtn");
		_shopBtn = GetNode<Button>("BottomNav/ShopBtn");
		_inventoryBtn = GetNode<Button>("BottomNav/InventoryBtn");

		_mapBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");
		_inventoryBtn.Pressed += () => GetTree().ChangeSceneToFile("res://scenes/inventory.tscn");

		_fishTypes = LoadConfig("res://configs/fish_types.json");
		var shopConfig = LoadConfig("res://configs/shop_config.json");
		_buyPriceMultiplier = shopConfig["buy_price_multiplier"].AsSingle();

		Refresh();
	}

	public void Refresh()
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		_coinsLabel.Text = $"Coins: {economy.Coins}";

		foreach (var child in _fishList.GetChildren())
			child.QueueFree();

		var availableFish = new Array();
		foreach (var key in _fishTypes.Keys)
		{
			var fishDef = (Dictionary)_fishTypes[key.ToString()];
			var card = CreateFishCard(fishDef);
			_fishList.AddChild(card);
		}
	}

	private Control CreateFishCard(Dictionary fishDef)
	{
		var panel = new PanelContainer();
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
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

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 12);

		var info = new VBoxContainer();
		var nameLabel = new Label { Text = fishDef["name"].ToString() };
		nameLabel.AddThemeFontSizeOverride("font_size", 18);

		var descLabel = new Label { Text = _fishTypes[fishDef["id"].ToString()].AsGodotDictionary()["description"].ToString() };
		descLabel.AddThemeColorOverride("font_color", new Color("#757575"));
		descLabel.AddThemeFontSizeOverride("font_size", 12);

		int price = Mathf.RoundToInt(fishDef["price"].AsInt32() * _buyPriceMultiplier);
		var priceLabel = new Label { Text = $"Price: {price} coins" };
		priceLabel.AddThemeColorOverride("font_color", new Color("#FFC107"));

		info.AddChild(nameLabel);
		info.AddChild(descLabel);
		info.AddChild(priceLabel);

		hbox.AddChild(info);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand });

		var buyBtn = new Button { Text = "Buy" };
		buyBtn.AddThemeFontSizeOverride("font_size", 14);
		buyBtn.CustomMinimumSize = new Vector2(72, 40);
		buyBtn.Pressed += () => OnBuyFish(fishDef["id"].ToString(), price);
		hbox.AddChild(buyBtn);

		panel.AddChild(hbox);
		return panel;
	}

	private void OnBuyFish(string fishType, int price)
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");

		if (!economy.SpendCoins(price))
		{
			GD.Print("[ShopUI] Not enough coins.");
			return;
		}

		if (inventory.GetFryCount(fishType) == 0 && !inventory.CanAddItem())
		{
			economy.AddCoins(price); // Refund
			GD.Print("[ShopUI] Inventory full.");
			return;
		}

		inventory.AddFry(fishType);
		GetNode<SaveManager>("/root/SaveManager").SaveGame();
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
