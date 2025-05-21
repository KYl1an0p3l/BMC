// RoomGenerator.cs
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RoomGenerator : Node2D {
    [Export] public int Width = 10;
    [Export] public int Height = 10;

    private TileData[,] grid;
    private Random _rnd = new Random();

    public override void _Ready() {
        TileDatabase.LoadAll();
    }

    public Node2D Generate() {
        // 1) cleanup
        foreach (var c in GetChildren()) c.QueueFree();
        grid = new TileData[Width, Height];

        // 2) choisissez vos positions d'entrée/sortie
        Vector2I start  = new Vector2I(0, _rnd.Next(Height));
        Vector2I finish = new Vector2I(Width - 1, _rnd.Next(Height));

        // 3) creuse un chemin principal (DFS simple)
        HashSet<Vector2I> path = CarveMainPath(start, finish);

        // 4) Place les tuiles du chemin principal
        PlacePathTiles(path, start, finish);

        // 5) remplissage progressif : BFS sur le chemin pour étendre au reste
        FloodFillFromPath(path);

        // 6) instanciation visuelle
        foreach (var pos in Iterate2D(Width, Height)) {
            var data = grid[pos.X, pos.Y];
            var inst = data.Scene.Instantiate<Node2D>();
            inst.Position = new Vector2(pos.X, pos.Y) * TILE_SIZE;
            AddChild(inst);
        }

        return this;
    }

    // DFS récursif pour tracer un chemin simple de start à finish
    private HashSet<Vector2I> CarveMainPath(Vector2I start, Vector2I finish) {
        var visited = new HashSet<Vector2I>();
        visited.Add(start);
        if (Dfs(start, finish, visited)) return visited;
        return new HashSet<Vector2I> { start, finish }; // fallback
    }

    private bool Dfs(Vector2I current, Vector2I target, HashSet<Vector2I> visited) {
        if (current == target) return true;
        var dirs = new List<Vector2I> {
            new Vector2I(1,0), new Vector2I(-1,0),
            new Vector2I(0,1), new Vector2I(0,-1)
        }.OrderBy(_ => _rnd.Next()).ToList();

        foreach (var d in dirs) {
            var nxt = current + d;
            if (nxt.X < 0 || nxt.Y < 0 || nxt.X >= Width || nxt.Y >= Height) continue;
            if (visited.Contains(nxt)) continue;
            visited.Add(nxt);
            if (Dfs(nxt, target, visited)) return true;
            visited.Remove(nxt);
        }
        return false;
    }

    // Place les tuiles d'entrée/sortie et du chemin, en filtrant pour ouverture sur ce chemin
    private void PlacePathTiles(HashSet<Vector2I> path, Vector2I start, Vector2I finish) {
        foreach (var cell in path) {
            bool isStart  = cell == start;
            bool isFinish = cell == finish;
            // déterminer contraintes : quelles directions du chemin passent par ici ?
            bool up    = path.Contains(cell + new Vector2I(0, -1));
            bool down  = path.Contains(cell + new Vector2I(0, +1));
            bool left  = path.Contains(cell + new Vector2I(-1,0));
            bool right = path.Contains(cell + new Vector2I(+1,0));
            // On interdit les ouvertures vers l'extérieur, sauf pour l’entrée/sortie :
            bool hasLeft   = cell.X > 0;
            bool hasRight  = cell.X < Width  - 1;
            bool hasTop    = cell.Y > 0;
            bool hasBottom = cell.Y < Height - 1;
            // sélectionner parmi les tiles qui ouvrent EXACTEMENT sur ces directions
            var candidates = TileDatabase.AllTiles
                .Where(td =>
                    // 1) connexion vers le chemin (ou parent dans le flood)
                    (!up    || td.OpenTop)    &&
                    (!down  || td.OpenBottom) &&
                    (!left  || td.OpenLeft)   &&
                    (!right || td.OpenRight)
                    // 2) pas de trou sur l’extérieur,
                //    sauf si start/finish autorisent la sortie
                && (td.OpenLeft   ? (hasLeft   || isStart)  : true)
                && (td.OpenRight  ? (hasRight  || isFinish) : true)
                && (!td.OpenTop    || hasTop)
                && (!td.OpenBottom || hasBottom)
                )
                .ToArray();
            if (candidates.Length == 0)
                GD.PrintErr($"Aucun candidat chemin en {cell}");
            //on place :
            grid[cell.X, cell.Y] = PickRandom(candidates);
        }
    }

    // Remplissage BFS : on étend le chemin pour couvrir toute la grille
    private void FloodFillFromPath(HashSet<Vector2I> path) {
        var queue = new Queue<Vector2I>(path);
        var filled = new HashSet<Vector2I>(path);

        while (queue.Count > 0) {
            var cell = queue.Dequeue();
            foreach (var d in new[]{ new Vector2I(1,0),new Vector2I(-1,0),
                                     new Vector2I(0,1),new Vector2I(0,-1) }) {
                var nxt = cell + d;
                if (!(nxt.X < 0 || nxt.Y < 0 || nxt.X >= Width || nxt.Y >= Height) && !(filled.Contains(nxt)))
                {
                    // pour chaque voisin non rempli, on contraint l'ouverture vers 'cell'
                    bool up = (d == new Vector2I(0, 1));
                    bool down = (d == new Vector2I(0, -1));
                    bool left = (d == new Vector2I(1, 0));
                    bool right = (d == new Vector2I(-1, 0));
                    // On interdit les ouvertures vers l'extérieur, sauf pour l’entrée/sortie :
                    bool hasLeftN = nxt.X > 0;
                    bool hasRightN = nxt.X < Width - 1;
                    bool hasTopN = nxt.Y > 0;
                    bool hasBottomN = nxt.Y < Height - 1;

                    // on ne se préoccupe pas des autres côtés (les laisse libres)
                    var candidates = TileDatabase.AllTiles
                        .Where(td =>
                            // 1) connexion vers le chemin (ou parent dans le flood)
                            (!up || td.OpenTop) &&
                            (!down || td.OpenBottom) &&
                            (!left || td.OpenLeft) &&
                            (!right || td.OpenRight)

                        // 2) aucun trou vers l’extérieur
                        && (!td.OpenLeft || hasLeftN)
                        && (!td.OpenRight || hasRightN)
                        && (!td.OpenTop || hasTopN)
                        && (!td.OpenBottom || hasBottomN)
                        )
                        .ToArray();
                    if (candidates.Length == 0)
                    {
                        GD.PrintErr($"Aucun candidat flood en {nxt}");
                        continue;
                    }
                    grid[nxt.X, nxt.Y] = PickRandom(candidates);
                    filled.Add(nxt);
                    queue.Enqueue(nxt);
                }
            }
        }
    }

    private T PickRandom<T>(T[] arr) => arr[_rnd.Next(arr.Length)];
    private IEnumerable<Vector2I> Iterate2D(int w,int h) {
        for(int x=0;x<w;x++)for(int y=0;y<h;y++)yield return new Vector2I(x,y);
    }
    private const int TILE_SIZE = 200;
}
