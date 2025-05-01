using Godot;
using System;

public partial class Enemy1 : CharacterBody2D
{
    [Export] public int Health = 3;
    [Export] public float Speed = 50f;
    [Export] public float Gravity = 800f;
    [Export] public float MaxFallSpeed = 200f;

    private Vector2 _velocity = Vector2.Zero;
    private Vector2 direction = Vector2.Left;

    private RayCast2D rayLeft;
    private RayCast2D rayRight;

    private bool isFrozen = false;
    private float freezeTimer = 0f;
    private const float FreezeDuration = 1.0f;

    public override void _Ready()
    {
        rayLeft = GetNode<RayCast2D>("RayLeft");
        rayRight = GetNode<RayCast2D>("RayRight");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isFrozen)
        {
            freezeTimer -= (float)delta;
            if (freezeTimer <= 0f)
                isFrozen = false;

            Velocity = new Vector2(0, Velocity.Y + Gravity * (float)delta);
            MoveAndSlide();
            return;
        }

        _velocity.Y += Gravity * (float)delta;
        if (_velocity.Y > MaxFallSpeed)
            _velocity.Y = MaxFallSpeed;

        _velocity.X = direction.X * Speed;
        Velocity = _velocity;
        MoveAndSlide();

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is Pp player)
            {
                // player.TakeDamage(1); // à activer quand le joueur a des PV
                isFrozen = true;
                freezeTimer = FreezeDuration;
                break;
            }
        }

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

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            QueueFree();
        }
    }

    private void FlipSprite()
    {
        if (HasNode("Sprite2D"))
        {
            var sprite = GetNode<Sprite2D>("Sprite2D");
            sprite.FlipH = direction == Vector2.Left;
        }
    }
}
