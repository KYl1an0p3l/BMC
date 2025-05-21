// WorldMap.cs
using Godot;
using System;

[Tool]
[GlobalClass]
public partial class WorldMap : Node
{
	/// La carte globale : 
	/// - "X" : salle courante
	/// - "P" : salle procédurale
	/// - tout autre string : nom de scène à charger (sans extension)
	public static WorldMap Instance { get; private set; }

	[Export] public Godot.Collections.Array<Godot.Collections.Array<string>> FlatMap;

	private string[,] _map;
	private Vector2I _current;
	private string _savedId;

	public override void _EnterTree()
	{
		Instance = this;
	}
	public override void _Ready()
	{
		if (FlatMap == null || FlatMap.Count == 0)
		{
			GD.Print("FlatMap n'est pas initialisé ou est vide !");
			return;
		}
		Build2DMap();
		// trouver automatiquement le "X" dans _map
		for (int y = 0; y < _map.GetLength(1); y++)
		{
			for (int x = 0; x < _map.GetLength(0); x++)
			{
				if (_map[x, y] == "X")
				{
					_current = new Vector2I(x, y);
					return;
				}
			}
		}
		GD.PrintErr("Aucune case 'X' trouvée dans la map !");
	}

	private void Build2DMap()
    {
        // nombre de lignes
        int h = FlatMap.Count;
        // nombre de colonnes (on suppose toutes lignes de même taille)
        int w = FlatMap[0].Count;

        _map = new string[w, h];

        for (int y = 0; y < h; y++)
        {
            var row = FlatMap[y];
            if (row == null || row.Count != w)
            {
                GD.PrintErr($"Ligne {y} de FlatMap est nulle ou de longueur différente !");
                continue;
            }

            for (int x = 0; x < w; x++)
            {
                _map[x, y] = row[x];
            }
        }
    }
	public string Move(Vector2I dir)
	{
		if (_map == null)
        {
            GD.PrintErr("WorldMap : _map n'est pas encore construite !");
            return null;
        }

		else if (_savedId == null)
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
