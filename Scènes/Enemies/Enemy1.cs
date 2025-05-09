using Godot;
using System;

public partial class Enemy1 : CharacterBody2D
{
    [Export] public int Health = 3;
    [Export] public float Speed = 150f;
    [Export] public float Gravity = 800f;
    [Export] public float MaxFallSpeed = 200f;

    private Vector2 _velocity = Vector2.Zero;
    private Vector2 direction = Vector2.Left;

    private RayCast2D rayLeft;
    private RayCast2D rayRight;

    private Pp overlappingPlayer = null;
    public override void _Ready()
    {
        rayLeft = GetNode<RayCast2D>("RayLeft");
        rayRight = GetNode<RayCast2D>("RayRight");

        var damageArea = GetNode<Area2D>("DamageArea");
        damageArea.BodyEntered += OnBodyEntered;
        damageArea.BodyExited += OnBodyExited;


        // Ne pas bloquer le joueur, uniquement collision avec le sol
        CollisionLayer = 1 << 2; // Layer 3 : Ennemi
        CollisionMask = 1 << 0;  // Mask 1 : Sol
    }

    public override void _Process(double delta)
    {
        if (overlappingPlayer != null && !overlappingPlayer.IsInvincible())
        {
            overlappingPlayer.TakeDamage();
        }
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

    private void OnBodyEntered(Node body)
    {
        if (body is Pp player)
        {
            overlappingPlayer = player;

            if (!player.IsInvincible())
            {
                player.TakeDamage();
            }
        }
    }
    private void OnBodyExited(Node body)
    {
        if (body == overlappingPlayer)
        {
            overlappingPlayer = null;
        }
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        GD.Print($"Vie restante de l'ennemi : {Health}");
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
