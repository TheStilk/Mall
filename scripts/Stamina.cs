using Godot;

namespace PolarBears.PlayerControllerAddon;

public partial class Stamina : Node
{
	[Export]
	public bool LimitlessSprint { get; set; } = false;
	[Export(PropertyHint.Range, "0,60,0.1,suffix:s,or_greater")]
	public float MaxSprintTime { get; set; } = 5.0f;
	// Regenerate run time multiplier (when run 10s and SprintTimeRegenerationMultiplier = 2.0f to full regenerate you need 5s)
	[Export(PropertyHint.Range, "0,10,0.01,or_greater")]
	public float SprintTimeRegenerationMultiplier { get; set; } = 1.0f;
	
	public float GetCurrentStamina() 
	{ 
		return MaxSprintTime - _currentRunTime; 
	}

	public float GetMaxStamina() 
	{ 
		return MaxSprintTime; 
	}

	public float GetStaminaPercentage() 
	{ 
		return (_currentRunTime / MaxSprintTime) * 100f; 
	}

	private float _currentRunTime;

	private float _walkSpeed;
	private float _sprintSpeed;

	public void SetSpeeds(float walkSpeed, float sprintSpeed)
	{
		_walkSpeed = walkSpeed;
		_sprintSpeed = sprintSpeed;
	}

	public float AccountStamina(double delta, float wantedSpeed)
	{
		if (LimitlessSprint) 
		{ 
			return wantedSpeed; 
		}

		if (Mathf.Abs(wantedSpeed - _sprintSpeed) > 0.1f)
		{
			float currentRegenMultiplier = SprintTimeRegenerationMultiplier; 
			
			if (wantedSpeed > 0.1f) 
			{ 
				currentRegenMultiplier /= 3.0f; 
			}
			
			float runtimeLeft = _currentRunTime - (currentRegenMultiplier * (float)delta);

			// Mathf.Clamp сам не даст _currentRunTime уйти ниже нуля.
			_currentRunTime = Mathf.Clamp(runtimeLeft, 0, MaxSprintTime); 

			return wantedSpeed;
		}

		// БЛОК ТРАТЫ ВЫНОСЛИВОСТИ (если пытаемся бежать)
		_currentRunTime = Mathf.Clamp(_currentRunTime + (float) delta, 0, MaxSprintTime);

		// Если выносливость кончилась (время бега >= макс.), возвращаем скорость ходьбы, иначе - желаемую (спринт)
		return _currentRunTime >= MaxSprintTime ? _walkSpeed : wantedSpeed;
	}
	
	public bool TrySpendStaminaOnJump()
	{
		float jumpCost = MaxSprintTime / 3.0f;
		float remainingStamina = MaxSprintTime - _currentRunTime;
		if (remainingStamina >= jumpCost)
		{
			_currentRunTime += jumpCost;
			_currentRunTime = Mathf.Clamp(_currentRunTime, 0, MaxSprintTime);
			return true;
		}
		else
		{
			return false;
		}
	}
}
