using Godot;
using Godot.Collections;

namespace CyberPond;

public partial class MainMapUI : Control
{
	private VBoxContainer _pondList;
	private Label _coinsLabel;
	private Label _emptyHint;
	private Button _createButton;
	private Control _createDialog;
	private LineEdit _nameInput;
	private Label _costLabel;

	// Fishing mode nodes
	private Control _fishingPanel;
	private Label _islandNameLabel;
	private Label _islandFishLabel;
	private Label _ticketCountLabel;
	private ColorRect _waterArea;
	private Button _leaveIslandBtn;

	private bool _isFishingMode;

	public override void _Ready()
	{
		_coinsLabel = GetNode<Label>("CoinsLabel");
		_pondList = GetNode<VBoxContainer>("PondScroll/PondList");
		_emptyHint = GetNode<Label>("EmptyHint");
		_createButton = GetNode<Button>("CreateButton");

		_createDialog = GetNode<Control>("CreateDialog");
		_nameInput = GetNode<LineEdit>("CreateDialog/NameInput");
		_costLabel = GetNode<Label>("CreateDialog/CostLabel");

		_fishingPanel = GetNode<Control>("FishingPanel");
		_islandNameLabel = GetNode<Label>("FishingPanel/IslandName");
		_islandFishLabel = GetNode<Label>("FishingPanel/IslandFish");
		_ticketCountLabel = GetNode<Label>("FishingPanel/TicketCount");
		_waterArea = GetNode<ColorRect>("FishingPanel/WaterArea");
		_leaveIslandBtn = GetNode<Button>("FishingPanel/LeaveBtn");

		_createButton.Pressed += OnCreatePressed;
		GetNode<Button>("CreateDialog/ConfirmCreate").Pressed += OnConfirmCreate;
		GetNode<Button>("CreateDialog/CancelCreate").Pressed += () => _createDialog.Hide();
		_leaveIslandBtn.Pressed += OnLeaveIslandPressed;

		// Water area click — start fishing (Phase 5.5)
		_waterArea.GuiInput += (e) =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
				OnWaterClicked();
		};

		// Bottom nav
		GetNode<Button>("BottomNav/NavButtons/DiscoverBtn").Pressed += () =>
		{
			if (_isFishingMode)
				ShowLeaveConfirmThen(() => GetTree().ChangeSceneToFile("res://scenes/discover.tscn"));
			else
				GetTree().ChangeSceneToFile("res://scenes/discover.tscn");
		};
		GetNode<Button>("BottomNav/NavButtons/ShopBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/shop.tscn");
		GetNode<Button>("BottomNav/NavButtons/InventoryBtn").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/inventory.tscn");

		// Check island state
		var islandManager = GetNode<IslandManager>("/root/IslandManager");
		if (islandManager.IsOnIsland)
			ShowLeaveConfirmThen(EnterFishingMode);
		else
			Refresh();
	}

	private void ShowLeaveConfirmThen(System.Action onCancel)
	{
		var islandManager = GetNode<IslandManager>("/root/IslandManager");
		var config = islandManager.GetIslandConfig(islandManager.CurrentIslandId);
		var islandName = config != null ? config["name"].AsString() : "当前岛屿";

		var dialog = new ConfirmationDialog();
		dialog.Title = "离开岛屿";
		dialog.DialogText = $"将要离开{islandName}，下次来需要再次购买船票，是否确认离开？";
		dialog.GetOkButton().Text = "确认离开";
		dialog.AddCancelButton("取消");

		dialog.Confirmed += () =>
		{
			islandManager.LeaveIsland();
			GetNode<SaveManager>("/root/SaveManager").SaveGame();
			Refresh();
		};
		dialog.Canceled += onCancel;

		AddChild(dialog);
		dialog.PopupCentered();
	}

	private void EnterFishingMode()
	{
		_isFishingMode = true;

		var islandManager = GetNode<IslandManager>("/root/IslandManager");
		var config = islandManager.GetIslandConfig(islandManager.CurrentIslandId);
		if (config == null) return;

		_islandNameLabel.Text = config["name"].AsString();
		_waterArea.Color = new Color(config["background_color"].AsString());

		var fishTypes = (Array)config["fish_types"];
		var fishNames = new System.Text.StringBuilder();
		var fishManager = GetNode<FishManager>("/root/FishManager");
		foreach (var ft in fishTypes)
		{
			var fishDef = fishManager.GetFishParams(ft.AsString());
			if (fishNames.Length > 0) fishNames.Append("、");
			fishNames.Append(fishDef["name"]);
		}
		_islandFishLabel.Text = $"可钓鱼类：{fishNames}";

		int tickets = islandManager.GetTicketCount(islandManager.CurrentIslandId);
		_ticketCountLabel.Text = $"剩余船票：{tickets} 张";

		// Hide pond UI, show fishing panel
		GetNode<ScrollContainer>("PondScroll").Visible = false;
		_emptyHint.Visible = false;
		_createButton.Visible = false;
		_fishingPanel.Show();
	}

	private void ExitFishingMode()
	{
		_isFishingMode = false;
		_fishingPanel.Hide();
		GetNode<ScrollContainer>("PondScroll").Visible = true;
		_createButton.Visible = true;
	}

	public void Refresh()
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		_coinsLabel.Text = $"Coins: {economy.Coins}";

		ExitFishingMode();

		var pondManager = GetNode<PondManager>("/root/PondManager");
		var ponds = pondManager.GetAllPonds();

		foreach (var child in _pondList.GetChildren())
			child.QueueFree();

		_emptyHint.Visible = ponds.Count == 0;

		foreach (var pond in ponds)
		{
			var card = CreatePondCard(pond);
			_pondList.AddChild(card);
		}
	}

	private Control CreatePondCard(PondData pond)
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
		var nameLabel = new Label { Text = pond.Name };
		nameLabel.AddThemeFontSizeOverride("font_size", 28);
		var fishCount = new Label { Text = $"Fish: {pond.Fishes.Count}" };
		fishCount.AddThemeColorOverride("font_color", new Color("#666666"));
		fishCount.AddThemeFontSizeOverride("font_size", 20);
		info.AddChild(nameLabel);
		info.AddChild(fishCount);

		hbox.AddChild(info);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand });

		var arrow = new Label { Text = ">" };
		arrow.AddThemeColorOverride("font_color", new Color("#90CAF9"));
		hbox.AddChild(arrow);
		panel.AddChild(hbox);

		panel.GuiInput += (e) =>
		{
			if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
			{
				var pondManager = GetNode<PondManager>("/root/PondManager");
				pondManager.SelectedPondId = pond.Id;
				GetTree().ChangeSceneToFile("res://scenes/pond_detail.tscn");
			}
		};

		return panel;
	}

	private void OnCreatePressed()
	{
		var pondManager = GetNode<PondManager>("/root/PondManager");
		if (!pondManager.CanCreateMorePonds())
		{
			GD.Print("[MainMapUI] Max ponds reached.");
			return;
		}

		var cost = pondManager.GetUnlockCost();
		var economy = GetNode<EconomyManager>("/root/EconomyManager");

		if (cost == 0)
			_costLabel.Text = "Free";
		else
			_costLabel.Text = $"Cost: {cost} coins (Balance: {economy.Coins})";

		_nameInput.Text = "";
		_createDialog.Show();
	}

	private void OnConfirmCreate()
	{
		var name = _nameInput.Text.StripEdges();
		if (string.IsNullOrEmpty(name))
			return;

		var pondManager = GetNode<PondManager>("/root/PondManager");
		if (!pondManager.CreatePond(name))
			return;

		GetNode<SaveManager>("/root/SaveManager").SaveGame();
		_createDialog.Hide();
		Refresh();
	}

	private void OnLeaveIslandPressed()
	{
		ShowLeaveConfirmThen(() => { }); // Cancel = stay
	}

	private FishingGame _activeFishing;

	private void OnWaterClicked()
	{
		if (_activeFishing != null) return;

		var islandManager = GetNode<IslandManager>("/root/IslandManager");
		var config = islandManager.GetIslandConfig(islandManager.CurrentIslandId);
		if (config == null) return;

		// Pick random fish from island's available types
		var fishTypes = (Array)config["fish_types"];
		string selectedFish = fishTypes[new System.Random().Next(fishTypes.Count)].AsString();

		var fishManager = GetNode<FishManager>("/root/FishManager");
		var fishParams = fishManager.GetFishParams(selectedFish);

		// Create fishing game overlay
		_activeFishing = new FishingGame();
		_activeFishing.LayoutMode = 1;
		_activeFishing.AnchorLeft = 0;
		_activeFishing.AnchorTop = 0;
		_activeFishing.AnchorRight = 1;
		_activeFishing.AnchorBottom = 1;
		_activeFishing.Setup(selectedFish, fishParams);
		_activeFishing.FishCaught += OnFishCaught;
		_activeFishing.FishFailed += OnFishFailed;

		// Close button
		var closeBtn = new Button { Text = "X 放弃" };
		closeBtn.LayoutMode = 0;
		closeBtn.OffsetRight = 80;
		closeBtn.OffsetLeft = 4;
		closeBtn.OffsetTop = 4;
		closeBtn.OffsetBottom = 36;
		closeBtn.AddThemeFontSizeOverride("font_size", 12);
		closeBtn.Pressed += CancelFishing;
		_activeFishing.AddChild(closeBtn);

		AddChild(_activeFishing);
		GD.Print($"[MainMapUI] Started fishing for {selectedFish}");
	}

	private void CancelFishing()
	{
		if (_activeFishing == null) return;
		_activeFishing.QueueFree();
		_activeFishing = null;
	}

	private void OnFishCaught(string fishType)
	{
		GD.Print($"[MainMapUI] Fish caught: {fishType}");
		_activeFishing?.QueueFree();
		_activeFishing = null;

		var inventory = GetNode<InventoryManager>("/root/InventoryManager");

		if (inventory.GetFryCount(fishType) == 0 && !inventory.CanAddItem())
		{
			ShowCatchFullDialog(fishType);
			return;
		}

		inventory.AddFry(fishType);
		GetNode<SaveManager>("/root/SaveManager").SaveGame();
	}

	private void OnFishFailed()
	{
		GD.Print("[MainMapUI] Fish got away!");
		_activeFishing?.QueueFree();
		_activeFishing = null;
	}

	private void ShowCatchFullDialog(string fishType)
	{
		var dialog = new ConfirmationDialog();
		dialog.Title = "背包已满";
		dialog.DialogText = "背包已满，是否出售钓到的鱼？取消后将放生。";

		dialog.Confirmed += () =>
		{
			var fishManager = GetNode<FishManager>("/root/FishManager");
			var fishParams = fishManager.GetFishParams(fishType);
			int price = fishParams["price"].AsInt32();
			GetNode<EconomyManager>("/root/EconomyManager").AddCoins(price);
			GetNode<SaveManager>("/root/SaveManager").SaveGame();
			GD.Print($"[MainMapUI] Sold caught {fishType} for {price} coins (inventory full).");
		};
		dialog.Canceled += () =>
		{
			GD.Print($"[MainMapUI] Released {fishType} (inventory full).");
		};

		AddChild(dialog);
		dialog.PopupCentered();
	}
}
