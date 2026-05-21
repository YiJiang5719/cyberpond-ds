using Godot;
using Godot.Collections;

namespace CyberPond;

public partial class PondDetailUI : Control
{
	private Label _titleLabel;
	private Label _fishCountLabel;
	private Label _roeLabel;
	private VBoxContainer _fishList;
	private Label _emptyHint;
	private Button _placeFryBtn;
	private Button _collectRoeBtn;
	private PondData _pond;

	private ColorRect _popupOverlay;
	private VBoxContainer _popupFryList;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("TitleLabel");
		_fishCountLabel = GetNode<Label>("FishCountLabel");
		_fishList = GetNode<VBoxContainer>("FishScroll/FishList");
		_emptyHint = GetNode<Label>("EmptyHint");

		GetNode<Button>("BackButton").Pressed += () =>
			GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");

		// Roe label: between FishCountLabel and FishScroll
		_roeLabel = new Label();
		_roeLabel.LayoutMode = 0;
		_roeLabel.OffsetLeft = 16;
		_roeLabel.OffsetTop = 116;
		_roeLabel.OffsetRight = 400;
		_roeLabel.OffsetBottom = 140;
		_roeLabel.AddThemeColorOverride("font_color", new Color("#66BB6A"));
		AddChild(_roeLabel);

		// Place Fry button
		_placeFryBtn = new Button { Text = "Place Fry" };
		_placeFryBtn.LayoutMode = 0;
		_placeFryBtn.OffsetLeft = 16;
		_placeFryBtn.OffsetTop = 404;
		_placeFryBtn.OffsetRight = 208;
		_placeFryBtn.OffsetBottom = 440;
		_placeFryBtn.Pressed += OnPlaceFry;
		AddChild(_placeFryBtn);

		// Collect Roe button
		_collectRoeBtn = new Button { Text = "Collect Roe" };
		_collectRoeBtn.LayoutMode = 0;
		_collectRoeBtn.OffsetLeft = 216;
		_collectRoeBtn.OffsetTop = 404;
		_collectRoeBtn.OffsetRight = 400;
		_collectRoeBtn.OffsetBottom = 440;
		_collectRoeBtn.Pressed += OnCollectRoe;
		AddChild(_collectRoeBtn);

		CreateFryPopup();
		CreateTestButtons();

		var pondManager = GetNode<PondManager>("/root/PondManager");
		_pond = pondManager.GetPond(pondManager.SelectedPondId);

		if (_pond == null)
		{
			GD.PrintErr("[PondDetailUI] Pond not found.");
			GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");
			return;
		}

		Refresh();
	}

	private void Refresh()
	{
		var fishManager = GetNode<FishManager>("/root/FishManager");
		fishManager.UpdateFishStates();

		_titleLabel.Text = _pond.Name;

		var pondManager = GetNode<PondManager>("/root/PondManager");
		int index = pondManager.GetAllPonds().IndexOf(_pond);
		int maxFish = pondManager.GetMaxFishCount(index);
		_fishCountLabel.Text = $"Fish: {_pond.Fishes.Count} / {maxFish}";

		int totalRoe = fishManager.GetUncollectedRoe(_pond);
		_roeLabel.Text = totalRoe > 0 ? $"Uncollected Roe: {totalRoe}" : "";
		_collectRoeBtn.Disabled = totalRoe == 0;
		_placeFryBtn.Disabled = _pond.Fishes.Count >= maxFish;

		foreach (var child in _fishList.GetChildren())
			child.QueueFree();

		_emptyHint.Visible = _pond.Fishes.Count == 0;

		foreach (var fish in _pond.Fishes)
			_fishList.AddChild(CreateFishEntry(fish, fishManager));
	}

	private Control CreateFishEntry(FishData fish, FishManager fishManager)
	{
		var button = new Button();
		button.SizeFlagsHorizontal = Control.SizeFlags.Fill;
		button.Flat = true;
		button.AddThemeStyleboxOverride("normal", new StyleBoxFlat
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
		hbox.MouseFilter = MouseFilterEnum.Ignore;
		hbox.AddThemeConstantOverride("separation", 8);

		var nameLabel = new Label { Text = fishManager.GetDisplayName(fish.FishType) };
		hbox.AddChild(nameLabel);
		hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand, MouseFilter = MouseFilterEnum.Ignore });

		var statusText = fishManager.GetGrowthRemaining(fish);
		var statusLabel = new Label { Text = statusText };
		statusLabel.AddThemeColorOverride("font_color",
			fish.IsAdult ? new Color("#4CAF50") : new Color("#FF9800"));
		statusLabel.AddThemeFontSizeOverride("font_size", 12);
		hbox.AddChild(statusLabel);

		if (fish.UncollectedRoe > 0)
		{
			var roeLabel = new Label { Text = $" +{fish.UncollectedRoe}" };
			roeLabel.AddThemeColorOverride("font_color", new Color("#66BB6A"));
			roeLabel.AddThemeFontSizeOverride("font_size", 12);
			hbox.AddChild(roeLabel);
		}

		button.AddChild(hbox);
		return button;
	}

	private void OnPlaceFry()
	{
		var inventory = GetNode<InventoryManager>("/root/InventoryManager");
		var fryDict = inventory.GetAllFry();
		GD.Print($"[PondDetailUI] OnPlaceFry: {fryDict.Count} fry types in inventory");

		foreach (var child in _popupFryList.GetChildren())
			child.QueueFree();

		bool hasFry = false;
		foreach (var kv in fryDict)
		{
			if (kv.Value <= 0) continue;
			hasFry = true;
			GD.Print($"[PondDetailUI] Adding fry option: {kv.Key} x{kv.Value}");
			_popupFryList.AddChild(CreateFryOption(kv.Key, kv.Value));
		}

		if (!hasFry)
		{
			GD.Print("[PondDetailUI] No fry available.");
			var hint = new Label { Text = "No fry in inventory.\nBuy some at the shop first!" };
			hint.AddThemeColorOverride("font_color", new Color("#757575"));
			hint.HorizontalAlignment = HorizontalAlignment.Center;
			_popupFryList.AddChild(hint);
		}

		_popupOverlay.Show();
	}

	private Control CreateFryOption(string fishType, int count)
	{
		var button = new Button();
		button.SizeFlagsHorizontal = Control.SizeFlags.Fill;
		button.CustomMinimumSize = new Vector2(0, 44);
		button.AddThemeStyleboxOverride("normal", new StyleBoxFlat
		{
			BgColor = new Color("#FFFFFF"),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 14,
			ContentMarginRight = 14,
			ContentMarginTop = 10,
			ContentMarginBottom = 10
		});
		button.AddThemeStyleboxOverride("hover", new StyleBoxFlat
		{
			BgColor = new Color("#E3F2FD"),
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
			ContentMarginLeft = 14,
			ContentMarginRight = 14,
			ContentMarginTop = 10,
			ContentMarginBottom = 10
		});

		var fishManager = GetNode<FishManager>("/root/FishManager");
		var label = new Label { Text = $"{fishManager.GetDisplayName(fishType)} Fry x{count}" };
		label.MouseFilter = MouseFilterEnum.Ignore;
		label.AddThemeColorOverride("font_color", new Color("#212121"));
		button.AddChild(label);

		button.Pressed += () =>
		{
			if (fishManager.PlaceFry(_pond.Id, fishType))
			{
				_popupOverlay.Hide();
				Refresh();
			}
		};

		return button;
	}

	private void OnCollectRoe()
	{
		var fishManager = GetNode<FishManager>("/root/FishManager");
		int collected = fishManager.CollectRoe(_pond.Id);
		if (collected > 0)
			Refresh();
	}

	private void CreateFryPopup()
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
			ContentMarginLeft = 20,
			ContentMarginRight = 20,
			ContentMarginTop = 16,
			ContentMarginBottom = 16
		});

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 8);

		var title = new Label { Text = "Select Fry to Place" };
		title.AddThemeFontSizeOverride("font_size", 16);

		_popupFryList = new VBoxContainer();
		_popupFryList.SizeFlagsHorizontal = Control.SizeFlags.Fill;
		_popupFryList.AddThemeConstantOverride("separation", 6);
		_popupFryList.CustomMinimumSize = new Vector2(0, 0);

		var closeBtn = new Button { Text = "Close" };
		closeBtn.CustomMinimumSize = new Vector2(80, 36);
		closeBtn.Pressed += () => _popupOverlay.Hide();

		vbox.AddChild(title);
		vbox.AddChild(_popupFryList);
		vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
		vbox.AddChild(closeBtn);

		panel.AddChild(vbox);
		_popupOverlay.AddChild(panel);
		AddChild(_popupOverlay);
	}

	private void CreateTestButtons()
	{
		var addCoinBtn = new Button { Text = "Add Coin" };
		addCoinBtn.LayoutMode = 0;
		addCoinBtn.OffsetRight = 404;
		addCoinBtn.OffsetLeft = 316;
		addCoinBtn.OffsetTop = 4;
		addCoinBtn.OffsetBottom = 36;
		addCoinBtn.Pressed += OnAddCoin;
		AddChild(addCoinBtn);

		var resetBtn = new Button { Text = "Reset" };
		resetBtn.LayoutMode = 0;
		resetBtn.OffsetRight = 308;
		resetBtn.OffsetLeft = 240;
		resetBtn.OffsetTop = 4;
		resetBtn.OffsetBottom = 36;
		resetBtn.Pressed += OnReset;
		AddChild(resetBtn);
	}

	private void OnAddCoin()
	{
		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		economy.AddCoins(5000);
		GetNode<SaveManager>("/root/SaveManager").SaveGame();
	}

	private void OnReset()
	{
		DirAccess.RemoveAbsolute("user://save_data.json");

		var economy = GetNode<EconomyManager>("/root/EconomyManager");
		economy.SetCoins(0);

		var inventory = GetNode<InventoryManager>("/root/InventoryManager");
		inventory.ResetAll();

		var pondManager = GetNode<PondManager>("/root/PondManager");
		pondManager.ResetAll();

		GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");
	}
}
