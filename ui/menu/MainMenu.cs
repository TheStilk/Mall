using Godot;

public partial class MainMenu : Control
{
	[Export] public string GameScenePath { get; set; } = "res://main.tscn"; 

	public override void _Ready()
	{
		var startButton = GetNode<Button>("CenterContainer/VBoxContainer/StartButton");
		var quitButton = GetNode<Button>("CenterContainer/VBoxContainer/ExitButton");

		if (startButton != null)
		{
			startButton.Pressed += OnStartButtonPressed;
		}
		else
		{
			GD.PrintErr("Ошибка: Узел StartButton не найден.");
		}

		if (quitButton != null)
		{
			quitButton.Pressed += OnExitButtonPressed;
		}
		else
		{
			GD.PrintErr("Ошибка: Узел QuitButton не найден.");
		}
	}

	private void OnStartButtonPressed()
	{
		GD.Print("Загрузка сцены: " + GameScenePath);
		if (ResourceLoader.Exists(GameScenePath))
		{
			//var root = GetTree().Root;
			GetTree().ChangeSceneToFile(GameScenePath);
		}
		else
		{
			GD.PrintErr($"Ошибка: Сцена не найдена по пути: {GameScenePath}");
		}
	}

	private void OnExitButtonPressed()
	{
		GetTree().Quit();
	}
}
