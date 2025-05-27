using Godot;
using System;

public partial class Camera2d : Camera2D
{
    public override void _Ready()
    {
        GD.Print("Camera Ready: ", Name);
        CallDeferred(nameof(_MakeCurrent));
    }
    public override void _Process(double delta)
    {
        // Si elle a un parent Node2D (ton PP), suis toujours sa position
        if (GetParent() is Node2D parent)
            GlobalPosition = parent.GlobalPosition;
    }
    private void _MakeCurrent()
    {
        MakeCurrent();
        LimitLeft   = int.MinValue;
        LimitTop    = int.MinValue;
        LimitRight  = int.MaxValue;
        LimitBottom = int.MaxValue;
    }

}
