using Godot;

// Этот скрипт является примером того, как можно создавать игровые системы, которые
// взаимодействуют с PlayerController. Этот скрипт применяет эффект низкой гравитации
// к любому PlayerController, который входит в Area3D. Это достигается путем изменения
// значения AdditionalGravityPower, принадлежащего дочернему элементу Gravity
// PlayerController.

namespace PolarBears.PlayerControllerAddon;

public partial class LowGravityArea3D : Area3D
{
	[Export] public float GravityReduction { set; get; } = 0.4f;

	public override void _Ready()
	{
		BodyEntered += (Node3D body) =>
		{
			if (body is PlayerController player) {
				player.Gravity.AdditionalGravityPower *= GravityReduction;
				GD.Print("Low Gravity Zone Entered");
			}
		};
		BodyExited += (Node3D body) =>
		{
			if (body is PlayerController player) {
				player.Gravity.AdditionalGravityPower /= GravityReduction;
				GD.Print("Low Gravity Zone Exited");
			}
		};
	}
}
