using Godot;
using System;
using PolarBears.PlayerControllerAddon;

public partial class MovingStatue : CharacterBody3D
{
	private const float Speed = 100.0f;
	private const float OccRayTargetYOffset = 0.5f;

	[ExportGroup("Target")]
	[Export] public CharacterBody3D TargetPlayer { get; set; }
	
	[ExportGroup("Attack Settings")]
	[Export(PropertyHint.Range, "0.1,10,0.1,suffix:m")]
	public float AttackRange { get; set; } = 1.0f;
	
	[Export(PropertyHint.Range, "0.01,5.0,0.01,suffix:s")]
	public float AttackInterval { get; set; } = 0.05f;
	
	[Export(PropertyHint.Range, "1,100,1")]
	public float AttackDamage { get; set; } = 50.0f;
	
	[ExportSubgroup("Sound")]
	[Export] public AudioStream AttackSoundEffect { get; set; }
	
	[Export] public AudioStream[] AttackSoundVariants { get; set; }
	
	[Export(PropertyHint.Range, "0.0,2.0,0.1")]
	public float AttackSoundVolume { get; set; } = 1.0f;
	
	public float Gravity { get; set; } = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");
	
	private System.Collections.Generic.List<RayCast3D> _occlusionCheckRays = new();
	
	private bool _isLookedAt = true;
	private bool _followPlayer = false;

	private Node3D _occlusionCheckRaysParent;
	private VisibleOnScreenNotifier3D _visibleOnScreenNotifier;
	private NavigationAgent3D _navAgent;
	
	// Attack system
	private Area3D _attackArea;
	private CollisionShape3D _attackCollisionShape;
	private AudioStreamPlayer3D _attackSound;
	private HealthSystem _playerHealthSystem;
	private double _timeSinceLastAttack = 0.0;
	private bool _playerInAttackRange = false;

	public override void _Ready()
	{
		GD.Print($"{Name}: Initializing MovingStatue");
		
		// Инициализация @onready переменных
		_occlusionCheckRaysParent = GetNode<Node3D>("OcclusionCheckRaysParent");
		_visibleOnScreenNotifier = GetNode<VisibleOnScreenNotifier3D>("VisibleOnScreenNotifier3D");
		_navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

		// Инициализация зоны атаки
		_attackArea = GetNodeOrNull<Area3D>("AttackArea");
		
		if (_attackArea != null)
		{
			_attackCollisionShape = _attackArea.GetNodeOrNull<CollisionShape3D>("AttackCollisionShape");
			
			// Подключаем сигналы Area3D
			_attackArea.BodyEntered += OnAttackAreaBodyEntered;
			_attackArea.BodyExited += OnAttackAreaBodyExited;
			
			// Устанавливаем размер зоны атаки
			if (_attackCollisionShape != null && _attackCollisionShape.Shape is SphereShape3D sphere)
			{
				sphere.Radius = AttackRange;
				GD.Print($"{Name}: Attack range set to {AttackRange}m");
			}
		}
		else
		{
			GD.PrintErr($"{Name}: AttackArea not found! Please add Area3D node named 'AttackArea' with CollisionShape3D child.");
		}

		_attackSound = GetNodeOrNull<AudioStreamPlayer3D>("AttackSound");
		
		if (_attackSound != null)
		{
			if (_attackSound.Stream == null && AttackSoundEffect != null)
			{
				_attackSound.Stream = AttackSoundEffect;
			}
			
			_attackSound.VolumeDb = Mathf.LinearToDb(AttackSoundVolume);
			GD.Print($"{Name}: Attack sound initialized");
		}
		else
		{
			GD.PrintErr($"{Name}: AttackSound (AudioStreamPlayer3D) not found! Add it as child node for sound effects.");
		}

		// 1. Проверка наличия цели
		if (TargetPlayer == null)
		{
			GD.PrintErr($"{Name} has no player target set in the TargetPlayer export property.");
			SetPhysicsProcess(false);
			return;
		}

		if (TargetPlayer is PlayerController playerController)
		{
			_playerHealthSystem = playerController.HealthSystem;
			
			if (_playerHealthSystem == null)
			{
				GD.PrintErr($"{Name}: Player's HealthSystem not found!");
			}
			else
			{
				GD.Print($"{Name}: Player HealthSystem connected successfully");
			}
		}
		else
		{
			GD.PrintErr($"{Name}: TargetPlayer is not a PlayerController!");
		}

		// 2. Сбор и настройка RayCast3D
		foreach (var node in _occlusionCheckRaysParent.GetChildren())
		{
			if (node is RayCast3D ray)
			{
				ray.AddException(this);
				ray.AddException(TargetPlayer);
				_occlusionCheckRays.Add(ray);
			}
		}

		GD.Print($"{Name}: Found {_occlusionCheckRays.Count} occlusion rays");

		// 3. Отложенный запуск следования
		CallDeferred(nameof(StartFollowingPlayer));
	}

	private void OnAttackAreaBodyEntered(Node3D body)
	{
		if (body == TargetPlayer)
		{
			_playerInAttackRange = true;
			GD.Print($"{Name}: Player entered attack range!");
		}
	}

	private void OnAttackAreaBodyExited(Node3D body)
	{
		if (body == TargetPlayer)
		{
			_playerInAttackRange = false;
			GD.Print($"{Name}: Player left attack range");
		}
	}

	private async void StartFollowingPlayer()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		_followPlayer = true;
		GD.Print($"{Name}: Started following player");
	}

public override void _PhysicsProcess(double delta)
{
	Vector3 velocity = Velocity;

	if (!IsOnFloor())
	{
		velocity.Y -= Gravity * (float)delta;
	}

	if (!_followPlayer || _playerHealthSystem == null)
	{
		velocity.X = 0;
		velocity.Z = 0;
		Velocity = velocity;
		MoveAndSlide();
		return;
	}

	if (_playerHealthSystem.IsDead())
	{
		velocity.X = 0;
		velocity.Z = 0;
		Velocity = velocity;
		MoveAndSlide();
		return;
	}

	_isLookedAt = IsViewed();

	if (_playerInAttackRange && !_isLookedAt)
	{
		_timeSinceLastAttack += delta;
		
		if (_timeSinceLastAttack >= AttackInterval)
		{
			AttackPlayer();
			_timeSinceLastAttack = 0.0;
		}
		
		velocity.X = 0;
		velocity.Z = 0;
		
		Vector3 lookTarget = TargetPlayer.GlobalPosition;
		lookTarget.Y = GlobalPosition.Y;
		if (!lookTarget.IsEqualApprox(GlobalPosition))
		{
			LookAt(lookTarget, Vector3.Up);
		}
	}
	else if (_isLookedAt)
	{
		velocity.X = 0;
		velocity.Z = 0;
	}
	else
	{
		Vector3 direction = Vector3.Zero;
		
		_navAgent.TargetPosition = TargetPlayer.GlobalPosition;
		
		direction = _navAgent.GetNextPathPosition() - GlobalPosition;
		direction.Y = 0; // Используем навигацию только для горизонтали
		direction = direction.Normalized();

		if (_navAgent.GetCurrentNavigationPath().Length > 0)
		{
			Vector3 whereToLook = _navAgent.GetNextPathPosition();
			whereToLook.Y = GlobalPosition.Y;

			if (!whereToLook.IsEqualApprox(GlobalPosition))
			{
				LookAt(whereToLook, Vector3.Up);
			}
		}

		velocity.X = direction.X * Speed;
		velocity.Z = direction.Z * Speed;
	}

	Velocity = velocity;
	MoveAndSlide();
}

	private void AttackPlayer()
	{
		if (_playerHealthSystem != null && !_playerHealthSystem.IsDead())
		{
			_playerHealthSystem.TakeDamage(AttackDamage);
			GD.Print($"{Name} attacked player for {AttackDamage} damage! Player HP: {_playerHealthSystem.GetCurrentHealth()}");
			PlayAttackSound();
		}
	}

	private void PlayAttackSound()
	{
		GD.Print($"{Name}: PlayAttackSound called");
		
		if (_attackSound == null)
		{
			GD.PrintErr($"{Name}: _attackSound is null!");
			return;
		}
		
		GD.Print($"{Name}: AudioStreamPlayer3D found");
		
		AudioStream soundToPlay = null;
		
		// Выбираем случайный звук из вариантов, если они есть
		if (AttackSoundVariants != null && AttackSoundVariants.Length > 0)
		{
			int randomIndex = GD.RandRange(0, AttackSoundVariants.Length - 1);
			soundToPlay = AttackSoundVariants[randomIndex];
			GD.Print($"{Name}: Using variant sound #{randomIndex}");
		}
		// Иначе используем основной звук
		else if (AttackSoundEffect != null)
		{
			soundToPlay = AttackSoundEffect;
			GD.Print($"{Name}: Using AttackSoundEffect");
		}
		// Или используем звук, установленный в ноде
		else if (_attackSound.Stream != null)
		{
			soundToPlay = _attackSound.Stream;
			GD.Print($"{Name}: Using Stream from node");
		}
		
		if (soundToPlay != null)
		{
			_attackSound.Stream = soundToPlay;
			
			// Если звук еще играет, перезапускаем его
			if (_attackSound.Playing)
			{
				_attackSound.Stop();
			}
			
			_attackSound.Play();
			GD.Print($"{Name}: Sound playing! Volume: {_attackSound.VolumeDb} dB, Max Distance: {_attackSound.MaxDistance}");
		}
		else
		{
			GD.PrintErr($"{Name}: No sound to play! Please assign audio in inspector.");
		}
	}

	private bool IsViewed()
	{
		bool viewed = _visibleOnScreenNotifier.IsOnScreen();
		
		if (!viewed)
		{
			return viewed;
		}
		
		int collidingRays = 0;

		foreach (var ray in _occlusionCheckRays)
		{
			Vector3 localTarget = ray.ToLocal(TargetPlayer.GlobalPosition);
			localTarget.Y += OccRayTargetYOffset;
			ray.TargetPosition = localTarget;
			
			ray.ForceRaycastUpdate();
			
			if (ray.IsColliding())
			{
				collidingRays++;
			}
		}

		if (collidingRays >= _occlusionCheckRays.Count)
		{
			viewed = false;
		}
		return viewed;
	}

	public override void _ExitTree()
	{
		// Отключаем сигналы при удалении
		if (_attackArea != null)
		{
			_attackArea.BodyEntered -= OnAttackAreaBodyEntered;
			_attackArea.BodyExited -= OnAttackAreaBodyExited;
		}
	}
}
