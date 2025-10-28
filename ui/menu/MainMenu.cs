using Godot;

public partial class MainMenu : Control
{
	[Export] public string GameScenePath { get; set; } = "res://main.tscn"; 

	public override void _Ready()
	{
		var startButton = GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
		var quitButton = GetNode<Button>("CenterContainer/VBoxContainer/ExitButton");

		if (startButton != null) { startButton.Pressed += OnStartButtonPressed; }
		else { GD.PrintErr("ERROR: Узел StartButton не найден."); }

		if (quitButton != null) { quitButton.Pressed += OnExitButtonPressed; }
		else { GD.PrintErr("ERROR: Узел QuitButton не найден."); }
	}

	private void OnStartButtonPressed()
	{	
		if (!IsInsideTree())
		{
			GD.PrintErr("MainMenu node is not inside the scene tree!");
			return;
		}
		GetTree().ChangeSceneToFile(GameScenePath);
	}

	private void OnExitButtonPressed()
	{
		GetTree().Quit();
	}
}
