using Godot;

namespace PolarBears.PlayerControllerAddon;

public partial class PlayerHUD : CanvasLayer
{
	[Export]
	public NodePath PlayerControllerPath { get; set; }
	
	private PlayerController _playerController;
	private HealthSystem _healthSystem;
	private Stamina _stamina;
	
	private ProgressBar _healthBar;
	private ProgressBar _staminaBar;
	private Label _healthLabel;
	private Label _staminaLabel;
	
	public override void _Ready()
	{
		GD.Print("PlayerHUD: _Ready called");
		
		_healthBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/HealthBar");
		_staminaBar = GetNode<ProgressBar>("MarginContainer/VBoxContainer/StaminaBar");
		_healthLabel = GetNode<Label>("MarginContainer/VBoxContainer/HealthBar/HealthLabel");
		_staminaLabel = GetNode<Label>("MarginContainer/VBoxContainer/StaminaBar/StaminaLabel");
		
		GD.Print("PlayerHUD: UI elements found");
		
		if (PlayerControllerPath != null && !PlayerControllerPath.IsEmpty)
		{
			GD.Print($"PlayerHUD: Using path {PlayerControllerPath}");
			_playerController = GetNode<PlayerController>(PlayerControllerPath);
		}
		else
		{
			// Try to find it in the scene tree
			GD.Print("PlayerHUD: Searching for PlayerController in scene tree");
			_playerController = GetTree().Root.FindChild("PlayerController", true, false) as PlayerController;
			
			// Alternative search methods
			if (_playerController == null)
			{
				_playerController = GetTree().Root.FindChild("Player", true, false) as PlayerController;
			}
			
			if (_playerController == null)
			{
				var nodes = GetTree().GetNodesInGroup("player");
				if (nodes.Count > 0)
				{
					_playerController = nodes[0] as PlayerController;
				}
			}
		}
		
		if (_playerController == null)
		{
			GD.PrintErr("PlayerHUD: PlayerController not found! Make sure PlayerController exists and path is correct.");
			GD.PrintErr("PlayerHUD: Try setting PlayerControllerPath in inspector or add PlayerController to 'player' group");
			return;
		}
		
		_healthSystem = _playerController.HealthSystem;
		_stamina = _playerController.Stamina;
		
		if (_healthSystem == null)
		{
			GD.PrintErr("PlayerHUD: HealthSystem is null!");
			return;
		}
		
		if (_stamina == null)
		{
			GD.PrintErr("PlayerHUD: Stamina is null!");
			return;
		}
		
		GD.Print($"PlayerHUD: HealthSystem found - Current HP: {_healthSystem.CurrentHealth}, Max HP: {_healthSystem.MaxHealth}");
		GD.Print($"PlayerHUD: Stamina found - Max Stamina: {_stamina.GetMaxStamina()}");
		
		_healthBar.MinValue = 0;
		_healthBar.MaxValue = _healthSystem.MaxHealth;
		_healthBar.Value = _healthSystem.CurrentHealth;
		
		_staminaBar.MinValue = 0;
		_staminaBar.MaxValue = _stamina.GetMaxStamina();
		_staminaBar.Value = _stamina.GetMaxStamina();
		
		GD.Print($"PlayerHUD: HealthBar setup - Min: {_healthBar.MinValue}, Max: {_healthBar.MaxValue}, Value: {_healthBar.Value}");
		GD.Print($"PlayerHUD: StaminaBar setup - Min: {_staminaBar.MinValue}, Max: {_staminaBar.MaxValue}, Value: {_staminaBar.Value}");
		
		// Connect signals using Godot 4 syntax
		_healthSystem.Connect(HealthSystem.SignalName.Damaged, Callable.From<float>(OnPlayerDamaged));
		_healthSystem.Connect(HealthSystem.SignalName.Died, Callable.From(OnPlayerDied));
		_healthSystem.Connect(HealthSystem.SignalName.FullyRecovered, Callable.From(OnPlayerFullyRecovered));
		
		UpdateHealthDisplay();
		UpdateStaminaDisplay();
		
		GD.Print("PlayerHUD: Initialization complete");
	}
	
	public override void _Process(double delta)
	{
		if (_playerController == null || _healthSystem == null || _stamina == null)
			return;
			
		UpdateHealthDisplay();
		UpdateStaminaDisplay();
	}
	
	private void UpdateHealthDisplay()
	{
		if (_healthSystem == null || _healthBar == null) return;
		
		float currentHealth = _healthSystem.GetCurrentHealth();
		float maxHealth = _healthSystem.MaxHealth;
		
		if (_healthBar.MaxValue != maxHealth)
		{
			_healthBar.MaxValue = maxHealth;
		}
		
		_healthBar.Value = currentHealth;
		
		if (_healthLabel != null)
		{
			_healthLabel.Text = $"Health: {Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
		}
		
		float healthPercent = currentHealth / maxHealth;
		
		if (healthPercent <= 0.25f)
		{
			_healthBar.Modulate = new Color(1, 0.3f, 0.3f); // Red
		}
		else if (healthPercent <= 0.5f)
		{
			_healthBar.Modulate = new Color(1, 0.7f, 0); // Orange
		}
		else
		{
			_healthBar.Modulate = Colors.White;
		}
	}
	
	private void UpdateStaminaDisplay()
	{
		if (_stamina.LimitlessSprint)
		{
			_staminaBar.Visible = false;
			return;
		}
		
		_staminaBar.Visible = true;
		float currentStamina = _stamina.GetCurrentStamina();
		_staminaBar.Value = currentStamina;
		_staminaLabel.Text = $"Stamina: {Mathf.RoundToInt(currentStamina)}/{Mathf.RoundToInt(_stamina.GetMaxStamina())}";
		
		if (currentStamina <= _stamina.GetMaxStamina() * 0.2f)
		{
			_staminaBar.Modulate = new Color(1, 0.5f, 0);
		}
		else
		{
			_staminaBar.Modulate = Colors.White;
		}
	}
	
	private void OnPlayerDamaged(float amount)
	{
		Tween tween = CreateTween();
		tween.TweenProperty(_healthBar, "modulate", new Color(1, 0, 0), 0.1);
		tween.TweenProperty(_healthBar, "modulate", Colors.White, 0.2);
	}
	
	private void OnPlayerDied()
	{
		_healthLabel.Text = "DEAD";
		_healthBar.Modulate = new Color(0.5f, 0, 0);
	}
	
	private void OnPlayerFullyRecovered()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(_healthBar, "modulate", new Color(0, 1, 0.5f), 0.2);
		tween.TweenProperty(_healthBar, "modulate", Colors.White, 0.3);
	}
	
	public override void _ExitTree()
	{
		if (_healthSystem != null && !IsQueuedForDeletion())
		{
			if (_healthSystem.IsConnected(HealthSystem.SignalName.Damaged, Callable.From<float>(OnPlayerDamaged)))
				_healthSystem.Disconnect(HealthSystem.SignalName.Damaged, Callable.From<float>(OnPlayerDamaged));
			
			if (_healthSystem.IsConnected(HealthSystem.SignalName.Died, Callable.From(OnPlayerDied)))
				_healthSystem.Disconnect(HealthSystem.SignalName.Died, Callable.From(OnPlayerDied));
			
			if (_healthSystem.IsConnected(HealthSystem.SignalName.FullyRecovered, Callable.From(OnPlayerFullyRecovered)))
				_healthSystem.Disconnect(HealthSystem.SignalName.FullyRecovered, Callable.From(OnPlayerFullyRecovered));
		}
	}
}
