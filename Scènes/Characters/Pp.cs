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
    private enum AttackDirection { Left, Right, Up, Down }
    private AttackDirection currentAttackDirection = AttackDirection.Right;
    private AttackDirection lastHorizontalDirection = AttackDirection.Right;
    private bool isDownwardAttack = false;
    private bool isAttacking = false;
    
    [Export] private float pogo_jump_power = -670f;
    [Export] private float pogo_jump_duration = 0.25f; 
    [Export] private float jump_power = -970f; // force 
    private float jump_elapsed = 0f;// chronomètre
    private bool isJumping = false;
    [Export] private float max_jump_hold_time = 0.25f;//durée max d'apui bouton
    private bool pogoJumped = false;
 

    private Vector2 velocity;
    private Vector2 screenSize;
    private bool LookingLeft = false;

    private int maxHealth = 3;
    private int currentHealth;
    private HBoxContainer heartsContainer;

    private bool isHitBoxTriggered = false;

    private Timer invincibilityTimer;
    [Export] private float knockback_jump_power = -500f;
    [Export] private float knockback_horizontal_force = 250f;
    private bool isKnockback = false;
    private bool knockbackJumped = false;
    private float knockback_elapsed = 0f;
    [Export] private float knockback_duration = 0.25f;



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

        currentHealth = maxHealth;
        heartsContainer = GetNode<HealthBar>("../CanvasLayer/HealthBar");
        ((HealthBar)heartsContainer).UpdateHearts(currentHealth);
    }   


    public override void _Process(double delta)
    {
        if (isKnockback)
        {
            HandleGravity(delta);
            return;
        }
        velocity = new Vector2();
        Sauter();
        UpdateAttackDirection();
        HandleMovement(delta);
        HandleGravity(delta);
        HandleAttack();
    }

    private void UpdateAttackDirection()
    {
        isDownwardAttack=false;
        Vector2 atkOffset;
        if (Input.IsActionPressed("z"))
        {
            currentAttackDirection = AttackDirection.Up;
        }
        else if (Input.IsActionPressed("s") && !IsOnFloor())
        {
            currentAttackDirection = AttackDirection.Down;
        }
        else if (Input.IsActionPressed("q"))
        {
            currentAttackDirection = AttackDirection.Left;
            lastHorizontalDirection = AttackDirection.Left;
        }
        else if (Input.IsActionPressed("d"))
        {
            currentAttackDirection = AttackDirection.Right;
            lastHorizontalDirection = AttackDirection.Right;
        }
        else
        {
            currentAttackDirection = lastHorizontalDirection;
        }
        switch (currentAttackDirection)
        {
            case AttackDirection.Up:
                atkOffset = new Vector2(5, -30);
                break;

            case AttackDirection.Down:
                atkOffset = new Vector2(5, 165);
                isDownwardAttack=true;
                break;

            case AttackDirection.Left:
                atkOffset = new Vector2(-70, 60);
                break;

            case AttackDirection.Right:
            default:
                atkOffset = new Vector2(85, 60);
                break;
        }
        var shape = zoneAtkCollision.Shape as RectangleShape2D;
        if (shape != null)
        {
            switch (currentAttackDirection)
            {
                case AttackDirection.Up:
                case AttackDirection.Down:
                    shape.Size = new Vector2(90, 60); // étroit et haut
                    break;

                case AttackDirection.Left:
                case AttackDirection.Right:
                default:
                    shape.Size = new Vector2(60, 90); // large et bas
                    break;
            }
        }
        zoneAtkArea.GlobalPosition = GlobalPosition + atkOffset;
    }

    private void Sauter(){
        if (IsOnFloor() && Input.IsActionJustPressed("jump")) {
            isJumping = true;
            jump_elapsed = 0f;
        }
    }
    private void HandleMovement(double delta)
    {
        if (isKnockback) return;
        velocity.X = 0;
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
        if (knockbackJumped)
        {
            knockback_elapsed += (float)delta;
            if (knockback_elapsed < knockback_duration)
            {
                velocity.Y = Mathf.Lerp(velocity.Y, knockback_jump_power, 1.0f);
            }
            else
            {
                knockbackJumped = false;
            }
        }
        else if (isJumping)
        {
            jump_elapsed += (float)delta;
            if (jump_elapsed < max_jump_hold_time && (Input.IsActionPressed("jump")))
            {
                velocity.Y = Mathf.Lerp(velocity.Y, jump_power, 1.0f);
            }
            else
            {
                isJumping = false;
            }
        }
        else if (pogoJumped)
        {
            jump_elapsed += (float)delta;
            if (jump_elapsed < pogo_jump_duration)
            {
                velocity.Y = Mathf.Lerp(velocity.Y, pogo_jump_power, 1.0f);
            }
            else
            {
                pogoJumped = false;
            }
        }
        else if (!IsOnFloor())
        {
            velocity.Y += (gravity / 2);
        }
        else
        {
            velocity.Y = 0;
        }


        Velocity = velocity;
        MoveAndSlide();
        velocity = Velocity;

        if (isKnockback && IsOnFloor())
        {
            isKnockback = false;
            invincibilityTimer.Start(); 
        }
    }

    private void HandleAttack()
    {
        if (isKnockback) return;
        if (Input.IsActionJustPressed("atk") && !isAttacking)
        {
            isAttacking = true;
            zoneAtkSprite.Visible = true;

            // Capture les ennemis présents au moment de l'attaque
            var initialTargets = new Godot.Collections.Array<Node>();
            foreach (var body in zoneAtkArea.GetOverlappingBodies())
            {
                if (body is Enemy1)
                    initialTargets.Add(body);
            }

            // Inflige les dégâts une seule fois
            foreach (var body in initialTargets)
            {
                if (body is Enemy1 enemy)
                {
                    enemy.TakeDamage(1);
                    if (isDownwardAttack)
                    {
                        jump_elapsed = 0f;
                        pogoJumped = true;
                    }
                }
            }

            // Timer pour désactiver la zone d'attaque et réactiver l'attaque
            var disableTimer = GetTree().CreateTimer(0.5f);
            disableTimer.Timeout += () =>
            {
                zoneAtkSprite.Visible = false;
                isAttacking = false;
            };
        }
    }

    public bool IsInvincible() => isHitBoxTriggered;

    public void TakeDamage(int amount)
    {
        if (isHitBoxTriggered || isKnockback)
            return;

        isHitBoxTriggered = true;
        isKnockback = true;
        knockbackJumped = true;
        knockback_elapsed = 0f;

        // On annule tout saut en cours
        isJumping = false;
        jump_elapsed = 0f;

        CollisionMask &= ~2u;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        ((HealthBar)heartsContainer).UpdateHearts(currentHealth);

        GD.Print($"Vie restante du joueur : {currentHealth}");

        // Knockback horizontal
        velocity.X = LookingLeft ? knockback_horizontal_force : -knockback_horizontal_force;

        if (currentHealth <= 0)
            QueueFree();
    }




    private void OnInvincibilityTimeout()
    {
        isHitBoxTriggered = false;
        CollisionMask |= 2u;
    }
}
