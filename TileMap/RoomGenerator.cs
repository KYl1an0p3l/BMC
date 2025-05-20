using Godot;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public partial class RoomGenerator : Node2D {
    [Export] public int Width = 10;
    [Export] public int Height = 10;

    private TileData[,] grid;

    public Node2D Generate()
    {

        foreach (var child in GetChildren())
            child.QueueFree();

        grid = new TileData[Width, Height];

        // 1) Place l’entrée et la sortie sur deux bords opposés
        var entrancePos = new Vector2I(0, RandomLine());
        var exitPos = new Vector2I(Width - 1, RandomLine());

        // Place des tuiles « ouvert vers l’intérieur » sur ces deux positions
        grid[entrancePos.X, entrancePos.Y] = PickRandom(TileDatabase.Filter(
            top: false,
            right: true,   // ouverture vers la droite
            bottom: false,
            left: true
        ));
        grid[exitPos.X, exitPos.Y] = PickRandom(TileDatabase.Filter(
            top: false,
            right: true,
            bottom: false,
            left: true    // ouverture vers la gauche
        ));

        // 2) Balayer chaque case
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (grid[x, y] != null) continue; // déjà l’entrée/sortie

                // Contrainte par rapport à la tuile de gauche
                bool needLeftOpen = (x > 0 && grid[x - 1, y]?.OpenRight == true);

                // Contrainte par rapport à la tuile du dessus
                bool needTopOpen = (y > 0 && grid[x, y - 1]?.OpenBottom == true);

                // Pour les bords, on interdit l’ouverture vers l’extérieur
                bool allowLeft = (x > 0);
                bool allowRight = (x < Width - 1);
                bool allowTop = (y > 0);
                bool allowBottom = (y < Height - 1);

                // Maintenant on filtre les tuiles possibles
                var candidates = TileDatabase.AllTiles.Where(td =>
                    (!needLeftOpen || td.OpenLeft) &&
                    (!needTopOpen || td.OpenTop) &&
                    (needLeftOpen || true) && // si pas besoin, on accepte both
                    (needTopOpen || true) &&
                    (Convert.ToInt32(td.OpenLeft) <= Convert.ToInt32(allowLeft)) && // interdit si bord
                    (Convert.ToInt32(td.OpenRight) <= Convert.ToInt32(allowRight)) &&
                    (Convert.ToInt32(td.OpenTop) <= Convert.ToInt32(allowTop)) &&
                    (Convert.ToInt32(td.OpenBottom) <= Convert.ToInt32(allowBottom))
                ).ToArray();

                // Si aucun candidat, on « back‑track » ou on repick globalement
                if (candidates.Length == 0)
                {
                    // Ici tu peux soit relancer Generate(), soit remédier case par case
                    GD.PrintErr("Pas de tuile compatible en ", x, y);
                }

                grid[x, y] = PickRandom(candidates);
            }
        }

        // 3) Instanciation visuelle
        foreach (var pos in Iterate2D(Width, Height))
        {
            var data = grid[pos.X, pos.Y];
            var inst = data.Scene.Instantiate<Node2D>();
            inst.Position = new Vector2(pos.X, pos.Y) * TILE_SIZE;
            AddChild(inst);
        }
        return this;
    }
    private TileData PickRandom(TileData[] arr)
        => arr[GD.Randi() % arr.Length];

    private int RandomLine() => (int)(GD.Randi() % Height);

    private IEnumerable<Vector2I> Iterate2D(int w, int h)
    {
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                yield return new Vector2I(x, y);
    }

    private const int TILE_SIZE = 200; // à adapter à ta grille
}
