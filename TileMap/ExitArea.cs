// ExitArea.cs
using Godot;
using System;

public partial class ExitArea : Area2D
{
    // On expose la direction de sortie pour pouvoir la régler en code lors de l'instanciation
    [Export] public Vector2I ExitDir { get; set; }
    private CharacterBody2D _player;
    private RoomManager _roomManager;

    public override void _Ready()
    {
        var root = GetTree().CurrentScene;
        _roomManager = root.GetNode<RoomManager>("RoomManager");
        SetDeferred("monitorable", true);
        Monitoring = true;
        CollisionLayer = 0;          // optionnel pour une Area passive
        CollisionMask = 2 << 0;      // pour détecter les objets en couche 2
        Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
    }

    public void SetPlayer(CharacterBody2D player)
    {
        _player = player;
    }

    private void OnBodyEntered(Node body)
    {
        GD.Print("entré !");
        if (body.IsInGroup("player"))
        {
            GD.Print("→ joueur détecté par groupe !");
            _roomManager.OnPlayerExited(ExitDir);
        }
        else
        {
            return;
        }

    }
    
}