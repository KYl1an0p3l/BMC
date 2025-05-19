using Godot;
using System;

public partial class Boss : CharacterBody2D
{
    [Export] public int Health = 6;
    [Export] public float Speed = 100f;
    [Export] public float Gravity = 800f;
    [Export] public float MaxFallSpeed = 400f;

    private Vector2 _velocity = Vector2.Zero;
    private Vector2 direction = Vector2.Left;
    private AnimatedSprite2D Sprite;
    private RayCast2D rayLeft;
    private RayCast2D rayRight;

    private Pp overlappingPlayer = null;

    private bool isMoving = true;
    private float stateTimer = 0f;

    public override void _Ready()
    {
        Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        rayLeft = GetNode<RayCast2D>("RayLeft");
        rayRight = GetNode<RayCast2D>("RayRight");

        var damageArea = GetNode<Area2D>("DamageArea");
        damageArea.BodyEntered += OnBodyEntered;
        damageArea.BodyExited += OnBodyExited;

        CollisionLayer = 1 << 2; // Layer 3 : Ennemi
        CollisionMask = 1 << 0;  // Mask 1 : Sol
    }

    public override void _Process(double delta)
    {
        if (overlappingPlayer != null && !overlappingPlayer.IsInvincible())
        {
            overlappingPlayer.TakeDamage(1);
        }

    }

    public override void _PhysicsProcess(double delta)
    {
        // Animation
        if (isMoving)
        {

            if (direction == Vector2.Left)
                Sprite.Play("left");
            else if (direction == Vector2.Right)
                Sprite.Play("right");
        }

        // Gravité
        _velocity.Y += Gravity * (float)delta;
        if (_velocity.Y > MaxFallSpeed)
            _velocity.Y = MaxFallSpeed;

        // Mouvement seulement si actif
        _velocity.X = isMoving ? direction.X * Speed : 0f;

        Velocity = _velocity;
        MoveAndSlide();
        _velocity = Velocity;

        if (isMoving)
        {
            // Changement de direction si vide devant
            if (direction == Vector2.Left && !rayLeft.IsColliding())
                direction = Vector2.Right;
            else if (direction == Vector2.Right && !rayRight.IsColliding())
                direction = Vector2.Left;
        }
    }
    private void OnBodyEntered(Node body)
    {
        if (body is Pp player)
        {
            overlappingPlayer = player;

            if (!player.IsInvincible())
                player.TakeDamage(1);
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
            QueueFree();
    }

}
