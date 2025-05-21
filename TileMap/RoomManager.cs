// RoomManager.cs
using Godot;
using System;

public partial class RoomManager : Node2D
{
    [Export] private NodePath playerPath;
    [Export] private Vector2 tileSize = new Vector2(200, 200);

    private RoomGenerator _proc;   // votre générateur semi‐procédural
    private Node2D _currentRoom;

    public override void _Ready()
    {
        _proc = GetNode<RoomGenerator>("RoomGenerator");
        // Charge la salle de départ (coord (0,0) dans WorldMap)
        CallDeferred("StartLoading");
        
    }
    private void StartLoading()
    {
        GD.Print("StartLoading called");
        LoadRoom(WorldMap.Instance.Move(new Vector2I(0, 0)));
    }

    public void OnPlayerExited(Vector2I exitDir)
    {
        // Détruit l’ancienne salle
        _currentRoom?.QueueFree();

        // Récupère l’identifiant de la salle suivante
        var roomId = WorldMap.Instance.Move(exitDir);

        // Charge la nouvelle
        LoadRoom(roomId);
    }

    private void LoadRoom(string roomId)
    {
        if (roomId == "P")
        {
            _currentRoom = _proc.Generate();
        }
        else
        {
            // Salle custom : charger la scène correspondante
            var scene = GD.Load<PackedScene>($"res://TileMap/Rooms/{roomId}.tscn");
            _currentRoom = scene.Instantiate<Node2D>();
        }

        AddChild(_currentRoom);

        // Crée dynamiquement les 4 sorties
        CreateExitArea("ExitTop", new Vector2(0, 0));
        CreateExitArea("ExitRight", new Vector2(1, 0));
        CreateExitArea("ExitBottom", new Vector2(0, 1));
        CreateExitArea("ExitLeft", new Vector2(0, 0));
    }

    private void CreateExitArea(string name, Vector2 dir)
    {
        var area = new ExitArea {Name = name, ExitDir = new Vector2I((int)dir.X, (int)dir.Y) };
        _currentRoom.AddChild(area);

        var shapeNode = new CollisionShape2D();
        var rect = new RectangleShape2D();
        if (name == "ExitRight" || name == "ExitLeft")
        {
            rect.Size = new Vector2(16, tileSize.Y * 10);
            shapeNode.Position = new Vector2(dir.X * tileSize.X * 10, 0);
        }
        else if (name == "ExitTop" || name == "ExitBottom")
        {
             rect.Size = new Vector2(tileSize.X * 10, 16);
            shapeNode.Position = new Vector2(0, dir.Y * tileSize.Y * 10);
        }

        shapeNode.Shape = rect;
        area.AddChild(shapeNode);
    }
}
