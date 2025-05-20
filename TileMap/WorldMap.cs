// WorldMap.cs
using Godot;
using System;

[Tool]
public partial class WorldMap : Node
{
	/// La carte globale : 
	/// - "X" : salle courante
	/// - "P" : salle procédurale
	/// - tout autre string : nom de scène à charger (sans extension)
	public static WorldMap Instance { get; private set; }

	[Export] public Godot.Collections.Array<string> FlatMap;
	[Export] public int MapWidth = 5;

	private string[,] _map;
	private Vector2I _current;
	private string _savedId;

	public override void _EnterTree()
	{
		Instance = this;
	}
	public override void _Ready()
	{
		Build2DMap();
		// trouver automatiquement le "X" dans _map
		for (int y = 0; y < _map.GetLength(1); y++)
			for (int x = 0; x < _map.GetLength(0); x++)
				if (_map[x, y] == "X")
					_current = new Vector2I(x, y);
	}

	private void Build2DMap()
	{
		int w = MapWidth;
		int h = FlatMap.Count / w;
		_map = new string[w, h];
		for (int i = 0; i < FlatMap.Count; i++)
			_map[i % w, i / w] = FlatMap[i];
	}
	public string Move(Vector2I dir)
	{
		if (_savedId == null)
		{
			_current = new Vector2I(0, 0);
			_savedId = _map[_current.X, _current.Y];
			_map[_current.X, _current.Y] = "X";
			return _savedId;
		}
		else
		{
			// 1) stocker l'ID courant
			_map[_current.X, _current.Y] = _savedId;

			// 2) calculer la nouvelle coord
			_current += dir;
			if (_current.X < 0 || _current.Y < 0 || _current.X >= _map.GetLength(0) || _current.Y >= _map.GetLength(1))
				throw new Exception("Sortie de la WorldMap !");

			// 3) mettre à jour le X
			_savedId = _map[_current.X, _current.Y];
			_map[_current.X, _current.Y] = "X";

			return _savedId;
		}

		
	}
}
