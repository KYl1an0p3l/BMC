using Godot;
using System;

public partial class RifleAtk : Area2D
{
    [Export] public float Speed = 100f;
    public Vector2 Direction = Vector2.Right;

    public override void _Ready()
    {
        Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += Direction * Speed * (float)delta;
    }

    private void OnBodyEntered(Node body)
    {
        if (body.IsInGroup("enemy") || body.IsInGroup("wall"))
        {
            QueueFree();
        }
    }
}
