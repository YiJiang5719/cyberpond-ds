using Godot;

namespace CyberPond;

/// Stardew-Valley-style fishing mini-game.
/// Player holds/taps to raise the catch zone, releases to let it fall.
/// Keep the fish inside the green zone to fill the progress bar.
public partial class FishingGame : Control
{
	// Fish config
	private string _fishType;
	private float _fishSpeed = 100f;
	private float _zoneRatio = 0.35f;
	private string _behavior = "smooth";
	private Color _fishColor = Colors.Red;

	// Bar geometry (fraction of control size)
	private const float BarLeft = 40f;
	private const float BarRight = 320f;
	private const float BarTop = 20f;
	private const float BarBottom = 420f;

	// State
	private float _fishPosition = 0.5f;       // 0=top, 1=bottom
	private float _fishTarget = 0.5f;
	private float _targetTimer;
	private float _catchPosition = 0.5f;       // top of catch zone (0-1)
	private float _catchVelocity;
	private float _catchProgress = 0.3f;       // starts at 30%
	private bool _playerHolding;
	private bool _gameOver;

	// Constants
	private const float CatchSpeed = 350f;     // pixels/sec when holding
	private const float Gravity = 600f;        // pixels/sec² when releasing
	private const float ProgressRate = 0.25f;  // fill per second
	private const float ProgressDecay = 0.1f;   // drain per second
	private const float FishHeight = 0.04f;    // fish visual height as fraction of bar

	[Signal] public delegate void FishCaughtEventHandler(string fishType);
	[Signal] public delegate void FishFailedEventHandler();

	private float BarHeight => BarBottom - BarTop;

	public void Setup(string fishType, Godot.Collections.Dictionary fishParams)
	{
		_fishType = fishType;
		_fishSpeed = fishParams["fishing_speed"].AsSingle();
		_zoneRatio = fishParams["fishing_zone_ratio"].AsSingle();
		_behavior = fishParams["fishing_behavior"].AsString();

		var colorHex = fishParams["color"].AsString();
		_fishColor = new Color(colorHex);

		_targetTimer = _behavior == "smooth" ? 1.5f : 0.5f;
		MouseFilter = MouseFilterEnum.Stop;
		GD.Print($"[FishingGame] Setup: {_fishType} speed={_fishSpeed} zone={_zoneRatio} behavior={_behavior}");
	}

	public override void _Process(double delta)
	{
		if (_gameOver) return;

		float dt = (float)delta;

		UpdateFish(dt);
		UpdateCatchZone(dt);
		UpdateProgress(dt);
		CheckGameOver();

		QueueRedraw();
	}

	private void UpdateFish(float dt)
	{
		_targetTimer -= dt;
		if (_targetTimer <= 0f)
		{
			_fishTarget = (float)GD.RandRange(0.1, 0.9);
			if (_behavior == "smooth")
				_targetTimer = (float)GD.RandRange(1.0, 2.5);
			else
				_targetTimer = (float)GD.RandRange(0.3, 0.8);
		}

		float step = _fishSpeed * dt;
		_fishPosition = Mathf.MoveToward(_fishPosition, _fishTarget, step);
	}

	private void UpdateCatchZone(float dt)
	{
		// Position 0=top, 1=bottom. Negative velocity = moving up.
		if (_playerHolding)
			_catchVelocity = -CatchSpeed;
		else
			_catchVelocity += Gravity * dt;

		_catchPosition += _catchVelocity * dt / BarHeight;
		_catchPosition = Mathf.Clamp(_catchPosition, 0f, 1f - _zoneRatio);
	}

	private void UpdateProgress(float dt)
	{
		float fishTop = _fishPosition - FishHeight;
		float fishBottom = _fishPosition + FishHeight;
		float zoneTop = _catchPosition;
		float zoneBottom = _catchPosition + _zoneRatio;

		bool fishInZone = fishBottom > zoneTop && fishTop < zoneBottom;

		if (fishInZone)
			_catchProgress += ProgressRate * dt;
		else
			_catchProgress -= ProgressDecay * dt;

		_catchProgress = Mathf.Clamp(_catchProgress, 0f, 1f);
	}

	private void CheckGameOver()
	{
		if (_catchProgress >= 1f)
		{
			_gameOver = true;
			EmitSignal(SignalName.FishCaught, _fishType);
		}
		else if (_catchProgress <= 0f)
		{
			_gameOver = true;
			EmitSignal(SignalName.FishFailed);
		}
	}

	public override void _Draw()
	{
		float barHeight = BarHeight;

		// Background
		DrawRect(new Rect2(BarLeft - 4, BarTop - 4, BarRight - BarLeft + 8, barHeight + 8),
			new Color(0.2f, 0.2f, 0.3f, 0.8f), true);
		DrawRect(new Rect2(BarLeft, BarTop, BarRight - BarLeft, barHeight),
			new Color(0.1f, 0.15f, 0.3f, 0.9f), true);

		// Catch zone (green)
		float zoneTopY = BarTop + _catchPosition * barHeight;
		float zoneHeight = _zoneRatio * barHeight;
		DrawRect(new Rect2(BarLeft + 2, zoneTopY, BarRight - BarLeft - 4, zoneHeight),
			new Color(0.2f, 0.9f, 0.3f, 0.5f), true);
		DrawRect(new Rect2(BarLeft + 2, zoneTopY, BarRight - BarLeft - 4, 2),
			new Color(0.3f, 1f, 0.4f, 0.9f), true); // top edge
		DrawRect(new Rect2(BarLeft + 2, zoneTopY + zoneHeight - 2, BarRight - BarLeft - 4, 2),
			new Color(0.3f, 1f, 0.4f, 0.9f), true); // bottom edge

		// Fish
		float fishY = BarTop + _fishPosition * barHeight;
		float fishH = FishHeight * barHeight;
		DrawRect(new Rect2(BarLeft + 10, fishY - fishH / 2, BarRight - BarLeft - 20, fishH),
			_fishColor, true);

		// Progress bar (top)
		float progressBarY = BarTop - 16;
		DrawRect(new Rect2(BarLeft, progressBarY, BarRight - BarLeft, 8),
			new Color(0.3f, 0.3f, 0.3f, 0.8f), true);
		DrawRect(new Rect2(BarLeft + 1, progressBarY + 1, (BarRight - BarLeft - 2) * _catchProgress, 6),
			_catchProgress > 0.5f ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.7f, 0.2f), true);

		// Instructions
		var font = GetThemeDefaultFont();
		if (font != null && !_gameOver)
		{
			DrawString(font, new Vector2(BarLeft + 10, BarBottom + 16),
				_catchProgress < 0.2f ? "!! 稳住 !!" : "按住屏幕升起钓竿",
				HorizontalAlignment.Left, BarRight - BarLeft - 20, 12, new Color(1, 1, 1, 0.7f));
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (_gameOver) return;

		if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
		{
			_playerHolding = mb.Pressed;
		}
	}
}
