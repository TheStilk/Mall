using Godot;

public partial class RandomGay : CharacterBody3D
{
	[Export]
	public float Speed = 1.5f;

	// Переменная для гравитации
	private float _gravity = (float)ProjectSettings.GetSetting("physics/3d/default_gravity");

	// Ноды
	private Godot.AnimationPlayer _animationPlayer;
	private Godot.NavigationAgent3D _navAgent;
	private Godot.Timer _timer;

	// Состояния
	private enum State { Idle, Walking }
	private State _state = State.Idle;

	[Export]
	private int _walkingDuration = 5;
	[Export]
	private int _idleDuration = 5;

	public override void _Ready()
	{
		_animationPlayer = GetNode<Godot.AnimationPlayer>("AnimationPlayer");
		_navAgent = GetNode<Godot.NavigationAgent3D>("NavigationAgent3D");
		_timer = GetNode<Godot.Timer>("Timer");

		_timer.Timeout += OnTimerTimeout;
		_timer.Start(_idleDuration);
		
		_animationPlayer.Play("Idle", 0.3f); 
	}

	public override void _Process(double delta)
	{
		// Проверка на null
		if (_navAgent == null || _animationPlayer == null) return;
		
		// --- ДОБАВЛЕНА ГРАВИТАЦИЯ ---
		// Получаем текущую Velocity
		Vector3 velocity = Velocity;

		// Применяем гравитацию, если не на полу
		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * (float)delta;
		}

		switch (_state)
		{
			case State.Walking:
				if (_navAgent.IsNavigationFinished())
				{
					// Дошли до цели
					_state = State.Idle;
					_timer.Start(_idleDuration);
					_animationPlayer.Play("Idle", 0.3f);
					
					// Обнуляем горизонтальную скорость
					velocity.X = 0;
					velocity.Z = 0;
				}
				else
				{
					// Еще идем, обновляем скорость из MoveTowardTarget
					velocity = MoveTowardTarget(Speed, velocity);
				}
				break;

			case State.Idle:
				_animationPlayer.Play("Idle", 0.3f);
				
				// Обнуляем горизонтальную скорость
				velocity.X = 0;
				velocity.Z = 0;
				break;
		}
		
		// Применяем итоговую скорость (с гравитацией)
		Velocity = velocity;
		MoveAndSlide();
	}
	
	// --- ФУНКЦИЯ ИЗМЕНЕНА ---
	private Vector3 MoveTowardTarget(float moveSpeed, Vector3 currentVelocity)
	{
		Vector3 nextPosition = _navAgent.GetNextPathPosition();
		
		// 1. ИСПРАВЛЕНИЕ ДВИЖЕНИЯ:
		// Считаем направление движения (только по XZ)
		Vector3 moveDirection = (nextPosition - GlobalTransform.Origin).Normalized();
		
		// Сохраняем вертикальную скорость (гравитацию)
		float verticalSpeed = currentVelocity.Y;
		
		// Задаем новую горизонтальную скорость
		Vector3 newVelocity = new Vector3(
			moveDirection.X * moveSpeed,
			verticalSpeed,
			moveDirection.Z * moveSpeed
		);

		// 2. ИСПРАВЛЕНИЕ ПОВОРОТА:
		// Проверяем, что есть куда двигаться
		if (moveDirection.Length() > 0)
		{
			// --- ЭТО ГЛАВНОЕ ИСПРАВЛЕНИЕ ДЛЯ ПОВОРОТА ---
			// Мы смотрим не НА цель, а В ПРОТИВОПОЛОЖНУЮ сторону
			// Это "обманывает" LookAt, чтобы он поворачивал модель (которая смотрит на +Z) правильно
			Vector3 lookTarget = GlobalTransform.Origin - moveDirection;
			
			Basis targetRotation = GlobalTransform.LookingAt(lookTarget, Vector3.Up).Basis;
			
			Transform3D newTransform = GlobalTransform;
			// Используем Orthonormalized() для избежания ошибок с масштабированием
			newTransform.Basis = newTransform.Basis.Orthonormalized().Slerp(targetRotation, 0.1f);
			GlobalTransform = newTransform;
		}

		// 3. АНИМАЦИЯ
		if (newVelocity.LengthSquared() > 0.1f) // LengthSquared() быстрее, чем Length()
		{
			_animationPlayer.Play("Walk", 0.3f);
		}

		return newVelocity;
	}

	private void OnTimerTimeout()
	{
		if (_navAgent == null || _animationPlayer == null) return;

		switch (_state)
		{
			case State.Idle:
				var randomTarget = new Vector3(
					(float)GD.RandRange(-10, 10), 
					0, 
					(float)GD.RandRange(-10, 10)
				);
				
				_navAgent.TargetPosition = randomTarget;
				_state = State.Walking;
				
				_timer.Start(_walkingDuration); 
				break;
			
			case State.Walking:
				_state = State.Idle;
				_timer.Start(_idleDuration);
				_animationPlayer.Play("Idle", 0.3f);
				break;
		}
	}
}
