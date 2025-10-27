using Godot;
using System;

// Важно: этот класс должен быть привязан к родительскому узлу (например, Node3D), 
// который содержит NavigationRegion3D и Terrain3D.
public partial class TerrainChunk : Node3D
{
	// --- ССЫЛКИ НА УЗЛЫ ---
	// Ссылку на Terrain3D будем хранить как универсальный GodotObject
	private GodotObject _terrainObject;
	private NavigationRegion3D _navRegion;

	// --- НАСТРОЙКИ ЗАПЕКАНИЯ ---
	[ExportGroup("Навигация")]
	[Export(PropertyHint.Range, "0,8,1")]
	public int LodForNavigation { get; set; } = 3;

	private MeshInstance3D _tempMeshInstance;

	public override void _Ready()
	{
		// 1. Получаем NavigationRegion3D
		_navRegion = GetNodeOrNull<NavigationRegion3D>("NavigationRegion3D");

		// 2. Получаем Terrain3D через его имя класса и IsClass
		Node terrainNode = GetNodeOrNull("Terrain3D");
		if (terrainNode != null && terrainNode.IsClass("Terrain3D"))
		{
			_terrainObject = terrainNode;
		}
		else
		{
			GD.PrintErr($"{Name}: Не найден дочерний узел 'Terrain3D' или он не является классом Terrain3D!");
			SetProcess(false); // Отключаем, если не нашли
			return;
		}

		if (_navRegion == null)
		{
			GD.PrintErr($"{Name}: Не найден дочерний узел 'NavigationRegion3D'!");
			SetProcess(false);
			return;
		}

		// 3. Запускаем генерацию после инициализации
		CallDeferred(nameof(GenerateNavMesh));
	}

	/// <summary>
	/// Основной метод для создания NavMesh из данных Terrain3D.
	/// </summary>
	public void GenerateNavMesh()
	{
		if (_terrainObject == null || _navRegion == null) return;
		
		GD.Print($"Terrain3D: Начинаю генерацию NavMesh (LOD: {LodForNavigation})...");

		// 1. Чистим старый временный меш, если он есть
		if (IsInstanceValid(_tempMeshInstance))
		{
			_tempMeshInstance.QueueFree();
		}

		// 2. ГЛАВНОЕ ИЗМЕНЕНИЕ: 
		// Вызываем метод BakeMesh() через Call(), передавая нужный LOD
		var result = _terrainObject.Call("bake_mesh", LodForNavigation);

		// Проверяем, что результат — это действительно Mesh (Godot.Mesh)
		if (result.Obj is Mesh terrainMesh)
		{
			// 3. Создаем ВРЕМЕННЫЙ узел MeshInstance3D для запекания
			_tempMeshInstance = new MeshInstance3D();
			_tempMeshInstance.Name = "TempNavMeshSource";
			_tempMeshInstance.Mesh = terrainMesh;

			// 4. Добавляем этот временный меш ВНУТРИ NavigationRegion3D
			_navRegion.AddChild(_tempMeshInstance);

			// 5. Запускаем запекание.
			// Используем синхронное запекание (false)
			_navRegion.BakeNavigationMesh(false); 
			
			GD.Print("Terrain3D: NavMesh успешно запечен.");

			// 6. Удаляем временный меш.
			_tempMeshInstance.QueueFree();
			// Важно: _tempMeshInstance = null; чтобы не было ошибок, если вызовем повторно
			_tempMeshInstance = null;
		}
		else
		{
			GD.PrintErr($"{Name}: Ошибка при вызове bake_mesh или он вернул не Mesh.");
		}
	}

	/// <summary>
	/// Вызывается при изменении блока.
	/// </summary>
	public void OnBlockChanged()
	{
		GD.Print("Terrain3D: Ландшафт изменен, запускаю пере-запекание NavMesh...");
		// В реальной игре нужно добавить задержку, чтобы избежать перегрузки.
		GenerateNavMesh();
	}
}
