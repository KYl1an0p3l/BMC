using Godot;
using System;

public partial class Enemy1 : CharacterBody2D
{
    [Export]
    public float Speed = 50f;

    [Export]
    public float Gravity = 800f;

    [Export]
    public float MaxFallSpeed = 200f;

    private Vector2 _velocity = Vector2.Zero;
    private Vector2 direction = Vector2.Left;

    private RayCast2D rayLeft;
    private RayCast2D rayRight;

    public override void _Ready()
    {
        rayLeft = GetNode<RayCast2D>("RayLeft");
        rayRight = GetNode<RayCast2D>("RayRight");
    }

    public override void _PhysicsProcess(double delta)
    {

        _velocity.Y += Gravity * (float)delta;
        if (_velocity.Y > MaxFallSpeed)
            _velocity.Y = MaxFallSpeed;

        _velocity.X = direction.X * Speed;

        Velocity = _velocity;
        MoveAndSlide();

        _velocity = Velocity;
        if (direction == Vector2.Left && !rayLeft.IsColliding())
        {
            direction = Vector2.Right;
            FlipSprite();
        }
        else if (direction == Vector2.Right && !rayRight.IsColliding())
        {
            direction = Vector2.Left;
            FlipSprite();
        }
    }

    private void FlipSprite()
    {
        var sprite = GetNode<Sprite2D>("Sprite2D"); 
        sprite.FlipH = direction == Vector2.Left;
    }
}
