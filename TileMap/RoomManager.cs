// RoomManager.cs
using Godot;
using System;

public partial class RoomManager : Node2D
{
    [Export] private NodePath playerPath;
    [Export] private Vector2 tileSize = new Vector2(200, 200);

    private RoomGenerator _proc;   // votre générateur semi‐procédural
    private Node2D _currentRoom;
    private CharacterBody2D _player;
    private Vector2I exitTaken;

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
        _player = _currentRoom.GetNode<CharacterBody2D>("DevTestMap/PP");
        
    }

    public void OnPlayerExited(Vector2I exitDir)
    {
        exitTaken = exitDir;
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
            AddChild(_currentRoom);
            if (_player.GetParent() != _currentRoom)
            {
                GD.Print("Ajout du joueur à la salle procédurale");
                _player.GetParent()?.RemoveChild(_player);
                var playerScene = GD.Load<PackedScene>("res://Scènes/Characters/PP.tscn");
                _player = playerScene.Instantiate<CharacterBody2D>();
                _currentRoom.CallDeferred("add_child",_player);
                if(exitTaken == new Vector2I(1, 0))
                    _player.GlobalPosition = _proc.VEntryWorldPos;
                if(exitTaken == new Vector2(-1, 0))
                    _player.GlobalPosition = _proc.VEndWorldPos;
                GD.Print($"Position du joueur : {_player.GlobalPosition}");

            }
            
        }
        else
        {
            // Salle custom : charger la scène correspondante
            var scene = GD.Load<PackedScene>($"res://TileMap/Rooms/{roomId}.tscn");
            _currentRoom = scene.Instantiate<Node2D>();
            CallDeferred("add_child", _currentRoom);
            if (roomId == "dev")
            {
                _player = _currentRoom.GetNode<CharacterBody2D>("DevTestMap/PP");
            }
            else if (_player.GetParent() != _currentRoom)
            {
                GD.Print("Ajout du joueur à la salle procédurale");
                _player.GetParent()?.RemoveChild(_player);
                var playerScene = GD.Load<PackedScene>("res://Scènes/Characters/PP.tscn");
                _player = playerScene.Instantiate<CharacterBody2D>();
                _currentRoom.CallDeferred("add_child",_player);
                if(exitTaken == new Vector2I(1, 0))
                    _player.GlobalPosition = _proc.VEntryWorldPos;
                if(exitTaken == new Vector2(-1, 0))
                    _player.GlobalPosition = _proc.VEndWorldPos;
                GD.Print($"Position du joueur : {_player.GlobalPosition}");

            }

        }

        // Crée dynamiquement les 4 sorties
        CreateExitArea("ExitTop", new Vector2(0, -1));
        CreateExitArea("ExitRight", new Vector2(1, 0));
        CreateExitArea("ExitBottom", new Vector2(0, 1));
        CreateExitArea("ExitLeft", new Vector2(-1, 0));
    }

    private void CreateExitArea(string name, Vector2 dir)
    {
        var area = new ExitArea {Name = name, ExitDir = new Vector2I((int)dir.X, (int)dir.Y) };
        area.SetPlayer(_player);
        _currentRoom.CallDeferred("add_child", area);

        if (name == "ExitTop") { dir.Y = 0; }
        else if (name == "ExitLeft") { dir.X = 0; }

        var shapeNode = new CollisionShape2D();
        var rect = new RectangleShape2D();
        if (name == "ExitRight" || name == "ExitLeft")
        {
            rect.Size = new Vector2(16, ((tileSize.Y * _proc.Height)*2));
            shapeNode.Position = new Vector2(dir.X * tileSize.X * _proc.Width - (8 + (-16*dir.X)), 0);
        }
        else if (name == "ExitTop" || name == "ExitBottom")
        {
             rect.Size = new Vector2(((tileSize.X * _proc.Width)*2), 16);
            shapeNode.Position = new Vector2(0, dir.Y * tileSize.Y * _proc.Height - (8 + (-16*dir.Y)));
        }

        if(name == "ExitTop"){ dir.Y = -1; }
        else if (name == "ExitLeft"){ dir.X = -1; }

        shapeNode.Shape = rect;
        area.CallDeferred("add_child", shapeNode);
    }
}
