// TileDatabase.cs
using Godot;
using System.Linq;
using System.IO;
using System.Collections.Generic;

public static class TileDatabase {
    public static TileData[] AllTiles { get; private set; }

    public static void LoadAll() {
        var tileFolderPath = "res://TileMap/Tiles";

        var dir = DirAccess.Open(tileFolderPath);
        if (dir == null) {
            GD.PrintErr($"Le dossier {tileFolderPath} est introuvable !");
            AllTiles = new TileData[0];
            return;
        }

        var tilePaths = new List<string>();

        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "") {
            if (dir.CurrentIsDir()) continue;
            if (fileName.EndsWith(".tres")) {
                var fullPath = $"{tileFolderPath}/{fileName}";
                tilePaths.Add(fullPath);
            }
        }
        dir.ListDirEnd();

        AllTiles = tilePaths
            .Select(path => GD.Load<TileData>(path))
            .Where(tile => tile != null)
            .ToArray();
    }

    // Filtre rapide par ouvertures :
    public static TileData[] Filter(bool top, bool right, bool bottom, bool left) {
        return AllTiles.Where(td =>
            td.OpenTop    == top    &&
            td.OpenRight  == right  &&
            td.OpenBottom == bottom &&
            td.OpenLeft   == left
        ).ToArray();
    }
}
