using Godot;

namespace CyberPond;

public partial class PondDetailUI : Control
{
    private Label _titleLabel;
    private Label _fishCountLabel;
    private VBoxContainer _fishList;
    private Label _emptyHint;
    private PondData _pond;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("TitleLabel");
        _fishCountLabel = GetNode<Label>("FishCountLabel");
        _fishList = GetNode<VBoxContainer>("FishScroll/FishList");
        _emptyHint = GetNode<Label>("EmptyHint");

        GetNode<Button>("BackButton").Pressed += () =>
            GetTree().ChangeSceneToFile("res://scenes/main_map.tscn");

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
        _titleLabel.Text = _pond.Name;

        var pondManager = GetNode<PondManager>("/root/PondManager");
        var allPonds = pondManager.GetAllPonds();
        int index = allPonds.IndexOf(_pond);
        int maxFish = pondManager.GetMaxFishCount(index);
        _fishCountLabel.Text = $"Fish: {_pond.Fishes.Count} / {maxFish}";

        foreach (var child in _fishList.GetChildren())
            child.QueueFree();

        _emptyHint.Visible = _pond.Fishes.Count == 0;

        foreach (var fish in _pond.Fishes)
            _fishList.AddChild(CreateFishEntry(fish));
    }

    private Control CreateFishEntry(FishData fish)
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

        var fishName = new Label { Text = GetDisplayName(fish.FishType) };
        hbox.AddChild(fishName);
        hbox.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.Expand });

        var status = fish.IsAdult ? "Adult" : "Growing...";
        var statusLabel = new Label { Text = status };
        statusLabel.AddThemeColorOverride("font_color",
            fish.IsAdult ? new Color("#4CAF50") : new Color("#FF9800"));
        hbox.AddChild(statusLabel);

        panel.AddChild(hbox);
        return panel;
    }

    private static string GetDisplayName(string typeKey) => typeKey switch
    {
        "common_carp" => "Common Carp",
        "goldfish" => "Goldfish",
        "koi" => "Koi",
        "arowana" => "Arowana",
        _ => typeKey
    };
}
