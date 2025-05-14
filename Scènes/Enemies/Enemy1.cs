using Godot;
using System;

public partial class Enemy1 : CharacterBody2D
{
    [Export] public int Health = 6;
    [Export] public float Speed = 100f;
    [Export] public float Gravity = 800f;
    [Export] public float MaxFallSpeed = 200f;

    private Vector2 _velocity = Vector2.Zero;
    private Vector2 direction = Vector2.Left;
    private AnimatedSprite2D Sprite;    
    private AudioStreamPlayer2D[] Sounds;
    private RayCast2D rayLeft;
    private RayCast2D rayRight;

    private Pp overlappingPlayer = null;

    // Nouveau : gestion du mouvement/arrêt
    private bool isMoving = true;
    private float stateTimer = 0f;
    private const float StateDuration = 1f;

    public override void _Ready()
    {
        GD.Randomize();
        Sounds = new AudioStreamPlayer2D[]
        {
            GetNode<AudioStreamPlayer2D>("Sound1"),
            GetNode<AudioStreamPlayer2D>("Sound2"),
            GetNode<AudioStreamPlayer2D>("Sound3")
        };
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

        stateTimer += (float)delta;
        if (stateTimer >= StateDuration)
        {
            isMoving = !isMoving;
            stateTimer = 0f;

            if (isMoving)
                PlayRandomSound();
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
        else
        {
            if (direction == Vector2.Left)
                Sprite.Play("idle_left");
            else if (direction == Vector2.Right)
                Sprite.Play("idle_right");
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
        private void PlayRandomSound()
    {
        int index = (int)(GD.Randi() % (ulong)Sounds.Length);
        Sounds[index].Play();
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

    private void FlipSprite()
    {
        if (HasNode("Sprite2D"))
        {
            var sprite = GetNode<Sprite2D>("Sprite2D");
            sprite.FlipH = direction == Vector2.Left;
        }
    }
}
