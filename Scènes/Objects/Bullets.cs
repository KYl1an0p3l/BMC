using Godot;
using System;

public partial class Bullets : Area2D
{
    [Export] public float Speed = 300f;
    private Vector2 _direction = Vector2.Right;

    public override void _Ready()
    {
        var notifier = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
        notifier.ScreenExited += OnScreenExited;
        Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));
    }

    public override void _PhysicsProcess(double delta)
    {
        
        Position += _direction * Speed * (float)delta;
        Rotation = _direction.Angle()+45;

    }
    private void OnBodyEntered(Node body)
    {
        if (body is Pp player)
        {
            player.TakeDamage(1);
            QueueFree();
        }
    }
    public void SetDirection(Vector2 dir)
    {
        _direction = dir.Normalized();
    }

    private void OnScreenExited()
    {
        QueueFree();
    }
}
