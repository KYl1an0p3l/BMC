// RoomGenerator.cs
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RoomGenerator : Node2D {
    [Export] public int Width = 7;
    [Export] public int Height = 5;

    private TileData[,] grid;
    private Random _rnd = new Random();
    public Vector2 VEntryWorldPos { get; private set; }
    public Vector2 VEndWorldPos { get; private set; }

    public override void _Ready()
    {
        TileDatabase.LoadAll();
    }

    public Node2D Generate()
    {
        var room = new Node2D();
        grid = new TileData[Width, Height];

        Vector2I start  = new Vector2I(0, _rnd.Next(Height));
        Vector2I finish = new Vector2I(Width - 1, _rnd.Next(Height));

        VEntryWorldPos = (new Vector2(start.X, start.Y) + new Vector2(0.5f, 0.5f)) * TILE_SIZE;
        VEndWorldPos = (new Vector2(finish.X, finish.Y) + new Vector2(0.5f, 0.5f)) * TILE_SIZE;

        HashSet<Vector2I> path = CarveMainPath(start, finish);
        PlacePathTiles(path, start, finish);
        FloodFillFromPath(path);

        foreach (var pos in Iterate2D(Width, Height))
        {
            var data = grid[pos.X, pos.Y];
            var inst = data.Scene.Instantiate<Node2D>();
            inst.Position = new Vector2(pos.X, pos.Y) * TILE_SIZE;
            room.AddChild(inst);
        }

        return room;
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
                    // 1) connexions requises pour suivre le chemin
                    (!up    || td.OpenTop)    &&
                    (!down  || td.OpenBottom) &&
                    (!left  || td.OpenLeft)   &&
                    (!right || td.OpenRight)

                    // 2) aucune ouverture vers l’extérieur, sauf exception entrée/sortie
                    && (td.OpenLeft   ? (hasLeft   || isStart)  : true)
                    && (td.OpenRight  ? (hasRight  || isFinish) : true)
                    && (!td.OpenTop    || hasTop)
                    && (!td.OpenBottom || hasBottom)

                    // 3) entrée : DOIT avoir OpenLeft ET au moins une autre ouverture (haut/bas/droite)
                    && (!isStart || (
                        td.OpenLeft &&
                        (td.OpenTop || td.OpenBottom || td.OpenRight)
                    ))

                    // 4) sortie : DOIT avoir OpenRight ET au moins une autre ouverture (haut/bas/gauche)
                    && (!isFinish || (
                        td.OpenRight &&
                        (td.OpenTop || td.OpenBottom || td.OpenLeft)
                    ))
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
                            // 1) connexion vers le parent/chemin
                            (!up || td.OpenTop) &&
                            (!down || td.OpenBottom) &&
                            (!left || td.OpenLeft) &&
                            (!right || td.OpenRight)

                            // 2) aucun trou vers l’extérieur
                            && (!td.OpenLeft || hasLeftN)
                            && (!td.OpenRight || hasRightN)
                            && (!td.OpenTop || hasTopN)
                            && (!td.OpenBottom || hasBottomN)

                            // 3) ne pas bloquer les ouvertures déjà existantes de ses voisins
                            && (
                                // voisin haut
                                !IsInBounds(nxt.X, nxt.Y - 1) || grid[nxt.X, nxt.Y - 1] == null || 
                                !grid[nxt.X, nxt.Y - 1].OpenBottom || td.OpenTop
                            )
                            && (
                                // voisin bas
                                !IsInBounds(nxt.X, nxt.Y + 1) || grid[nxt.X, nxt.Y + 1] == null || 
                                !grid[nxt.X, nxt.Y + 1].OpenTop || td.OpenBottom
                            )
                            && (
                                // voisin gauche
                                !IsInBounds(nxt.X - 1, nxt.Y) || grid[nxt.X - 1, nxt.Y] == null || 
                                !grid[nxt.X - 1, nxt.Y].OpenRight || td.OpenLeft
                            )
                            && (
                                // voisin droit
                                !IsInBounds(nxt.X + 1, nxt.Y) || grid[nxt.X + 1, nxt.Y] == null || 
                                !grid[nxt.X + 1, nxt.Y].OpenLeft || td.OpenRight
                            )
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

    private bool IsInBounds(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height;

    private const int TILE_SIZE = 200;
}
