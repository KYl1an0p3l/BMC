using Godot;
using System;

public partial class Pp : CharacterBody2D
{
    [Export] private int SPEED = 200;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    [Export] private int MAX_FALL_SPEED = 50;

    private AnimatedSprite2D animatedSprite;
    private CollisionShape2D collisionShape2D;

    private Area2D zoneAtkArea;
    private CollisionShape2D zoneAtkCollision;
    private Sprite2D zoneAtkSprite;

    [Export] private float jump_time = 0.25f; // durée de la force jumppower 
    [Export] private float jump_power = -970f; // force 
    private float jump_elapsed = 0f;// chronomètre
    private bool isJumping = false;
    [Export] private float max_jump_hold_time = 0.25f;//durée max d'apui bouton 

    private Vector2 velocity;
    private Vector2 screenSize;
    private bool LookingLeft = false;

    private int currentHealth = 3;
    private bool isHitBoxTriggered = false;

    private Timer invincibilityTimer;

    public override void _Ready()
    {
        screenSize = GetViewportRect().Size;

        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");

        zoneAtkArea = GetNode<Area2D>("ZoneAtk");

        zoneAtkCollision = zoneAtkArea.GetNode<CollisionShape2D>("CollisionShape2D");
        zoneAtkSprite = zoneAtkArea.GetNode<Sprite2D>("Sprite2D");
        

        invincibilityTimer = GetNode<Timer>("InvincibilityTimer");
        invincibilityTimer.Timeout += OnInvincibilityTimeout;

        zoneAtkArea.Monitoring = true;
        zoneAtkArea.Monitorable = true;
        zoneAtkSprite.Visible = false;

        CollisionLayer = 1 << 1; // couche 2 : joueur
        CollisionMask = 1 << 0;  // couche 1 : sol

    }   


    public override void _Process(double delta)
    {
        velocity = new Vector2();
        Sauter();
        HandleMovement(delta);
        HandleGravity(delta);
        HandleAttack();
    }

    private void Sauter(){
        if (IsOnFloor() && Input.IsActionJustPressed("ui_accept")) {
            isJumping = true;
            jump_elapsed = 0f;
        }
    }
    private void HandleMovement(double delta)
    {
        if (Input.IsActionPressed("d"))
        {
            velocity.X++;
            LookingLeft = false;
        }

        if (Input.IsActionPressed("q"))
        {
            velocity.X--;
            LookingLeft = true;
        }

        if (velocity.Length() > 0)
        {
            velocity = velocity.Normalized() * SPEED;
            animatedSprite.Play("gauche");
        }
        else
        {
            animatedSprite.Stop();
        }

        animatedSprite.Animation = "gauche";
        animatedSprite.FlipH = velocity.X > 0;

        Position += velocity * (float)delta;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 0, screenSize.X),
            Mathf.Clamp(Position.Y, 0, screenSize.Y)
        );
    }

    private void HandleGravity(double delta)
    {
        if (isJumping)
        {
            jump_elapsed += (float)delta; 

            if (jump_elapsed < max_jump_hold_time && Input.IsActionPressed("ui_accept"))
            {
                velocity.Y = Mathf.Lerp(velocity.Y, jump_power, 1.0f);
            }
            else
            {
                isJumping = false;
            }

        }
        else if (!IsOnFloor()) {
        velocity.Y += (gravity/2);
        }
        else {
            velocity.Y = 0;
        }

        Velocity = velocity;
        MoveAndSlide();
        velocity = Velocity;
    }

    private void HandleAttack()
    {
        if (Input.IsActionJustPressed("atk"))
        {
            // Définir la position d’attaque
            Vector2 atkOffset;
            if (Input.IsActionPressed("z"))
            {
                atkOffset = new Vector2(0, -30);
            }
            else if (Input.IsActionPressed("s") && !IsOnFloor())
            {
                atkOffset = new Vector2(0, 165);
            }
            else if (Input.IsActionPressed("q"))
            {
                atkOffset = new Vector2(-85, 60);
            }
            else if (Input.IsActionPressed("d"))
            {
                atkOffset = new Vector2(85, 60);
            }
            else
            {
                atkOffset = LookingLeft ? new Vector2(-85, 60) : new Vector2(85, 60);
            }

            zoneAtkArea.GlobalPosition = GlobalPosition + atkOffset;
            GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = true;

            // Maintenant, on détecte immédiatement les corps
            var bodies = zoneAtkArea.GetOverlappingBodies();
            GD.Print("Corps détectés : ", bodies.Count);
            foreach (var body in bodies)
            {
                GD.Print("Touché : ", body.Name);
                if (body is Enemy1 enemy)
                {
                    enemy.TakeDamage(1);
                }
            }

            // Désactive zone et visuel après courte durée (attaque visuelle)
            var disableTimer = GetTree().CreateTimer(0.1f);
            disableTimer.Timeout += () =>
            {
                GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = false;
            };
        }
    }

    public bool IsInvincible() => isHitBoxTriggered;

    public void TakeDamage(int amount)
    {
        if (isHitBoxTriggered)
            return;

        isHitBoxTriggered = true;
        CollisionMask &= ~2u;
        invincibilityTimer.Start();

        currentHealth -= amount;
        GD.Print($"Vie restante du joueur : {currentHealth}");
        if (currentHealth <= 0)
        {
            QueueFree();
        }

    }

    private void OnInvincibilityTimeout()
    {
        isHitBoxTriggered = false;
        CollisionMask |= 2u;
    }
}
