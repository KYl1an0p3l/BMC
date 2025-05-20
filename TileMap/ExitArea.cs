// ExitArea.cs
using Godot;
using System;

public partial class ExitArea : Area2D
{
    // On expose la direction de sortie pour pouvoir la régler en code lors de l'instanciation
    [Export] public Vector2I ExitDir { get; set; }

    public override void _Ready()
    {
        Monitorable = true;
        Monitoring  = true;
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node body)
    {
        if (body.Name != "Pp") 
            return;

        // On retrouve notre RoomManager parent et on lui signale la sortie
        if (GetParent() is RoomManager rm)
            rm.OnPlayerExited(ExitDir);
    }
}
