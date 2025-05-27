using Godot;
using System;

public partial class Enemy1 : CharacterBody2D
{
    [Export] public Ennemies ennemy;
    private AnimatedSprite2D Sprite;    
    private AudioStreamPlayer2D[] Sounds;
    private RayCast2D rayLeft;
    private RayCast2D rayRight;
    private RayCast2D rayWallLeft;
    private RayCast2D rayWallRight;

    private Pp overlappingPlayer = null;

    // Nouveau : gestion du mouvement/arrêt
    private float stateTimer = 0f;
    private const float StateDuration = 1f;

    public override void _Ready()
    {
        Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        rayLeft = GetNode<RayCast2D>("RayLeft");
        rayRight = GetNode<RayCast2D>("RayRight");
        rayWallLeft = GetNode<RayCast2D>("RayWallLeft");
        rayWallRight = GetNode<RayCast2D>("RayWallRight");

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
            ennemy.isMoving = !ennemy.isMoving;
            stateTimer = 0f;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Animation
        if (ennemy.isMoving)
        {
            if (ennemy.direction == Vector2.Left)
                Sprite.Play("left");
            else if (ennemy.direction == Vector2.Right)
                Sprite.Play("right");
        }
        else
        {
            if (ennemy.direction == Vector2.Left)
                Sprite.Play("idle_left");
            else if (ennemy.direction == Vector2.Right)
                Sprite.Play("idle_right");
        }

        // Gravité
        ennemy._velocity.Y += ennemy.Gravity * (float)delta;
        if (ennemy._velocity.Y > ennemy.MaxFallSpeed)
            ennemy._velocity.Y = ennemy.MaxFallSpeed;

        // Mouvement seulement si actif
        ennemy._velocity.X = ennemy.isMoving ? ennemy.direction.X * ennemy.Speed : 0f;

        Velocity = ennemy._velocity;
        MoveAndSlide();
        ennemy._velocity = Velocity;

        if (ennemy.isMoving)
        {
            if (ennemy.direction == Vector2.Left)
            {
                bool ignoreRay = false;
                if (rayLeft.IsColliding())
                {
                    var collider = rayLeft.GetCollider() as Node;
                    if (collider != null && collider.Name == "Test_Tile")
                        ignoreRay = true;
                }

                if (!ignoreRay && (!rayLeft.IsColliding() || rayWallLeft.IsColliding()))
                    ennemy.direction = Vector2.Right;
            }
            else if (ennemy.direction == Vector2.Right)
            {
                bool ignoreRay = false;
                if (rayRight.IsColliding())
                {
                    var collider = rayRight.GetCollider() as Node;
                    if (collider != null && collider.Name == "Test_Tile")
                        ignoreRay = true;
                }

                if (!ignoreRay && (!rayRight.IsColliding() || rayWallRight.IsColliding()))
                    ennemy.direction = Vector2.Left;
            }
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
        ennemy.Health -= amount;
        GD.Print($"Vie restante de l'ennemi : {ennemy.Health}");
        if (ennemy.Health <= 0)
            QueueFree();
    }

}
