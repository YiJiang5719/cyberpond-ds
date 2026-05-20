using Godot;

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

    public override void _Ready()
    {
        _coinsLabel = GetNode<Label>("CoinsLabel");
        _pondList = GetNode<VBoxContainer>("PondScroll/PondList");
        _emptyHint = GetNode<Label>("EmptyHint");
        _createButton = GetNode<Button>("CreateButton");

        _createDialog = GetNode<Control>("CreateDialog");
        _nameInput = GetNode<LineEdit>("CreateDialog/NameInput");
        _costLabel = GetNode<Label>("CreateDialog/CostLabel");

        _createButton.Pressed += OnCreatePressed;
        GetNode<Button>("CreateDialog/ConfirmCreate").Pressed += OnConfirmCreate;
        GetNode<Button>("CreateDialog/CancelCreate").Pressed += () => _createDialog.Hide();

        GetNode<Button>("BottomNav/NavButtons/ShopBtn").Pressed += () =>
            GetTree().ChangeSceneToFile("res://scenes/shop.tscn");
        GetNode<Button>("BottomNav/NavButtons/InventoryBtn").Pressed += () =>
            GetTree().ChangeSceneToFile("res://scenes/inventory.tscn");

        Refresh();
    }

    public void Refresh()
    {
        var economy = GetNode<EconomyManager>("/root/EconomyManager");
        _coinsLabel.Text = $"Coins: {economy.Coins}";

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
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        var fishCount = new Label { Text = $"Fish: {pond.Fishes.Count}" };
        fishCount.AddThemeColorOverride("font_color", new Color("#666666"));
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
        if (!pondManager.CreatePond(name, 0, 0))
            return;

        GetNode<SaveManager>("/root/SaveManager").SaveGame();
        _createDialog.Hide();
        Refresh();
    }
}
