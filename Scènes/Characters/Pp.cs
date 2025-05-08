using Godot;
using System;

public partial class Pp : CharacterBody2D
{
    [Export] private int SPEED = 200;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    [Export] private int MAX_FALL_SPEED = 30;

    [Export] private float jump_time = 0.25f;
    [Export] private float jump_power = -970f;
    private float jump_elapsed = 0f;
    private bool isJumping = false;
    [Export] private float max_jump_hold_time = 0.25f;

//SAUT COMPTEUR 
    private int jumpCount = 0;
    [Export] private int maxJumps = 2;




    // DASH
    [Export] private float dashSpeed = 600f; //longueur
    [Export] private float dashDuration = 0.2f;
    [Export] private float dashCooldown = 0.5f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.Zero;

    private AnimatedSprite2D animatedSprite;
    private CollisionShape2D collisionShape2D;
    private Vector2 velocity;
    private Vector2 screenSize;
    private CollisionShape2D zone_atk;
    private Area2D Test_hitBoxArea;
    private bool IsAttacking, isHitBoxTriggered = false;
    private bool LookingLeft = false;
    private int maxHealth, currentHealth = 3;

    public override void _Ready()
    {
        screenSize = GetViewportRect().Size;
        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        zone_atk = GetNode<CollisionShape2D>("ZoneAtk/CollisionShape2D");
        Test_hitBoxArea = GetNode<Area2D>("../../HurtBox/hitBox");
        Test_hitBoxArea.BodyEntered += OnHitBoxBodyEntered;
    }



    public override void _Process(double delta)
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= (float)delta;

        if (isDashing)
        {
            dashTimer -= (float)delta;
            if (dashTimer <= 0)
            {
                isDashing = false;
                Velocity = Vector2.Zero;
            }
            else
            {
                Velocity = dashDirection * dashSpeed;
                MoveAndSlide();
                return; // Empêche les autres actions pendant le dash
            }
        }
        else if (Input.IsKeyPressed(Key.F) && dashCooldownTimer <= 0)//touche f 
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            if (Input.IsActionPressed("q"))
                dashDirection = Vector2.Left;
            else if (Input.IsActionPressed("d"))
                dashDirection = Vector2.Right;
            else
                dashDirection = LookingLeft ? Vector2.Left : Vector2.Right;
        }



        velocity = new Vector2();
        Sauter();
        Mouvements_Limits(delta);
        Marche();
        gravity_gestion(delta);
        Attaque();
    }

    private void Marche()
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
        if (velocity.X != 0)
        {
            animatedSprite.Animation = "gauche";
            animatedSprite.FlipH = velocity.X > 0;
        }
    }

    private void Mouvements_Limits(double delta)
    {
        Position += velocity * (float)delta;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 0, screenSize.X),
            Mathf.Clamp(Position.Y, 0, screenSize.Y)
        );
        Mathf.Clamp(velocity.Y, -10, MAX_FALL_SPEED);
    }

    private void gravity_gestion(double delta)
    {
        if (isJumping)
        {
            jump_elapsed += (float)delta;
            if (jump_elapsed < max_jump_hold_time && Input.IsActionPressed("ui_accept"))
            {
                velocity.Y = Mathf.Lerp(velocity.Y, jump_power, 0.9f);
            }
            else
            {
                isJumping = false;
            }
        }
        else if (!IsOnFloor())
        {
            velocity.Y += Mathf.Lerp(gravity / 3, gravity, 0.5f);
        }
        else
        {
            velocity.Y = 0;
            jumpCount = 0; //remis à 0 QUAND ON RETOUCHE LE SOL 
        }

        Velocity = velocity;
        MoveAndSlide();
        velocity = Velocity;
    }

    private void Attaque()
    {
        zone_atk.SetDisabled(true);
        GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = false;
        if (Input.IsActionJustPressed("atk"))
        {
            zone_atk.SetDisabled(false);
            GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = true;

            Vector2 pos = LookingLeft ? new Vector2(-85, 60) : new Vector2(85, 60);
            if (Input.IsActionPressed("z"))
                pos = new Vector2(0, -30);
            else if (Input.IsActionPressed("s") && !IsOnFloor())
                pos = new Vector2(0, 165);
            else if (Input.IsActionPressed("q"))
                pos = new Vector2(-85, 60);
            else if (Input.IsActionPressed("d"))
                pos = new Vector2(85, 60);

            GetNode<Area2D>("ZoneAtk").Position = pos;
        }
    }

    private void OnHitBoxBodyEntered(Node body)
    {
        if (body == this)
        {
            currentHealth = Mathf.Max(0, currentHealth - 1);
            GD.Print("Vie restante : " + currentHealth);
        }
    }

   private void Sauter(){

    if (Input.IsActionJustPressed("ui_accept") && jumpCount < maxJumps)
    {
        velocity.Y = jump_power;
        isJumping = true;
        jump_elapsed = 0f;
        jumpCount++;
    }
}
}
