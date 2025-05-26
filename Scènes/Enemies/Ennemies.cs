using Godot;
using System;


[Tool]
[GlobalClass]
public partial class Ennemies : Resource
{
    [Export] public int Health;
    [Export] public float Speed;
    [Export] public float Gravity;
    [Export] public float MaxFallSpeed;

    [Export] public Vector2 _velocity = Vector2.Zero;
    [Export] public Vector2 direction = Vector2.Left;
    [Export] public bool isMoving = true;
}
