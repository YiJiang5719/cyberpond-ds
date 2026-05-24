using Godot;
using Godot.Collections;

namespace CyberPond;

public partial class DiscoverUI : Control
{
	private VBoxContainer _islandList;
	private Label _coinsLabel;

	// Island detail panel
	private ColorRect _islandDetail;
	private Label _detailName;
	private Label _detailDesc;
	private Label _detailFishLabel;
	private Label _detailTicketLabel;
	private Button _detailVisitBtn;

	private string _selectedIslandId;
	private Dictionary _shopConfig;

	public override void _Ready()
	{
		_coinsLabel = GetNode<Label>("CoinsLabel");
		_islandList = GetNode<VBoxContainer>("IslandScroll/IslandList");

		_islandDetail = GetNode<ColorRect>("IslandDetail");
		_detailName = GetNode<Label>("IslandDetail/DetailName");
		_detailDesc = GetNode<Label>("IslandDetail/DetailDesc");
		_detailFishLabel = GetNode<Label>("IslandDetail/DetailFishLabel");
		_detailTicketLabel = GetNode<Label>("IslandDetail/DetailTicketLabel");
		_detailVisitBtn = GetNode<Button>("IslandDetail/DetailVisitBtn");

		GetNode<Button>("IslandDetail/DetailBackBtn").Pressed += HideIslandDetail;
		_detailVisitBtn.Pressed += OnVisitIsland;

		// Bottom nav
		GetNode<Button>("BottomNav/NavButtons/PondBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");
		// DiscoverBtn is current tab — no scene change
		GetNode<Button>("BottomNav/NavButtons/ShopBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/shop.tscn");
		GetNode<Button>("BottomNav/NavButtons/InventoryBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/inventory.tscn");

		_shopConfig = LoadConfig("res://configs/shop_config.json");
		Refresh();
	}

	public void Refresh()
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		_coinsLabel.Text = $"Coins: {economy.Coins}";

		foreach (var child in _islandList.GetChildren())
			child.QueueFree();

		var islandManager = GetNode<IslandManager>("/root/IslandManager");
		var islands = islandManager.GetAllIslands();

		foreach (Dictionary island in islands)
		{
			_islandList.AddChild(CreateIslandCard(island));
		}
	}

	private Control CreateIslandCard(Dictionary islandDef)
	{
		var card = new Button();
		card.SizeFlagsHorizontal = Control.SizeFlags.Fill;
		card.CustomMinimumSize = new Vector2(0, 80);
		card.Flat = true;

		string bgColor = islandDef["background_color"].AsString();
		card.AddThemeStyleboxOverride("normal", new StyleBoxFlat
		{
			BgColor = new Color(bgColor),
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomLeft = 10,
			CornerRadiusBottomRight = 10,
			ContentMarginLeft = 16,
			ContentMarginRight = 16,
			ContentMarginTop = 12,
			ContentMarginBottom = 12
		});

		var hbox = new HBoxContainer();
		hbox.MouseFilter = MouseFilterEnum.Ignore;
		hbox.AddThemeConstantOverride("separation", 12);

		var info = new VBoxContainer();
		info.MouseFilter = MouseFilterEnum.Ignore;

		var nameLabel = new Label { Text = islandDef["name"].AsString() };
		nameLabel.AddThemeFontSizeOverride("font_size", 32);

		var descLabel = new Label { Text = islandDef["description"].AsString() };
		descLabel.AddThemeColorOverride("font_color", new Color("#424242"));
		descLabel.AddThemeFontSizeOverride("font_size", 22);

		info.AddChild(nameLabel);
		info.AddChild(descLabel);

		hbox.AddChild(info);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand, MouseFilter = MouseFilterEnum.Ignore });

		var arrow = new Label { Text = ">" };
		arrow.AddThemeColorOverride("font_color", new Color("#1E88E5"));
		arrow.AddThemeFontSizeOverride("font_size", 32);
		hbox.AddChild(arrow);

		card.AddChild(hbox);

		var islandId = islandDef["id"].AsString();
		card.Pressed += () => ShowIslandDetail(islandId);

		return card;
	}

	private void ShowIslandDetail(string islandId)
	{
		_selectedIslandId = islandId;

		var islandManager = GetNode<IslandManager>("/root/IslandManager");
		var islandConfig = islandManager.GetIslandConfig(islandId);

		_detailName.Text = islandConfig["name"].AsString();
		_detailDesc.Text = islandConfig["description"].AsString();

		var fishTypes = (Array)islandConfig["fish_types"];
		var fishNames = new System.Text.StringBuilder();
		var fishManager = GetNode<FishManager>("/root/FishManager");
		foreach (var ft in fishTypes)
		{
			var fishDef = fishManager.GetFishParams(ft.AsString());
			if (fishNames.Length > 0) fishNames.Append("、");
			fishNames.Append(fishDef["name"]);
		}
		_detailFishLabel.Text = $"可钓鱼类：{fishNames}";

		int ticketCount = islandManager.GetTicketCount(islandId);
		_detailTicketLabel.Text = $"持有船票：{ticketCount} 张";

		_islandDetail.Show();
	}

	private void HideIslandDetail()
	{
		_islandDetail.Hide();
		Refresh();
	}

	private void OnVisitIsland()
	{
		var islandManager = GetNode<IslandManager>("/root/IslandManager");
		int ticketCount = islandManager.GetTicketCount(_selectedIslandId);

		if (ticketCount <= 0)
		{
			GD.Print($"[DiscoverUI] No ticket for island {_selectedIslandId}");
			return;
		}

		if (!islandManager.ConsumeTicket(_selectedIslandId))
		{
			GD.Print($"[DiscoverUI] Failed to consume ticket for {_selectedIslandId}");
			return;
		}

		islandManager.CurrentIslandId = _selectedIslandId;
		GetNode<SaveManager>("/root/SaveManager").SaveGame();
		GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");
	}

	private Dictionary LoadConfig(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var json = new Json();
		json.Parse(file.GetAsText());
		return json.Data.AsGodotDictionary();
	}
}
