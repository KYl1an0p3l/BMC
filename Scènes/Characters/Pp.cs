using Godot;
using System;

public partial class Pp : CharacterBody2D
{
    [Export] private int SPEED = 200;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    [Export] private int MAX_FALL_SPEED = 50;

    private AnimatedSprite2D animatedSprite;
    private CollisionShape2D collisionShape2D;

    private Area2D zoneAtkArea, zone_get_rifle, zoneRifleAtkArea;
    
    private CollisionShape2D zone_atk, zone_atk_rifle, rifleGetDisable;
    private AnimatedSprite2D zoneAtkSprite,zoneRifleSprite;
    private enum AttackDirection { Left, Right, Up, Down }
    private AttackDirection currentAttackDirection = AttackDirection.Right;
    private AttackDirection lastHorizontalDirection = AttackDirection.Right;
    private bool isDownwardAttack = false;
    private bool isAttacking = false;
    
    [Export] private float pogo_jump_power = -670f;
    [Export] private float pogo_jump_duration = 0.25f; 
    [Export] private float jump_power = -770f; // force 
    private float jump_elapsed = 0f;// chronomètre
    private bool isJumping = false;
    [Export] private float max_jump_hold_time = 0.25f;//durée max d'apui bouton
    private bool pogoJumped = false;
    private int jumpCount = 0;
    [Export] private int maxJumps = 2;

    [Export] private float dashSpeed = 800f; 
    [Export] private float dashDuration = 0.2f;
    [Export] private float dashCooldown = 0.5f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection = Vector2.Zero;

    private Vector2 velocity;
    private Vector2 screenSize;
    private bool LookingLeft = false;

    [Export] private int maxHealth = 3;
    private int currentHealth;
    private HBoxContainer heartsContainer;
    private HealthBar healthBar;

    private bool isHitBoxTriggered = false;

    private Timer invincibilityTimer;
    [Export] private float knockback_jump_power = -500f;
    [Export] private float knockback_horizontal_force = 250f;
    private bool isKnockback = false;
    private bool knockbackJumped = false;
    private float knockback_elapsed = 0f;
    [Export] private float knockback_duration = 0.25f;

    private DeadScreen deadScreen;
    private bool hasGun, isDead = false;
    private int bulletsFired = 0;
    private int maxBullets = 6;
    private bool isReloading = false;
    private Timer reloadTimer;    
    private Area2D parryArea;
    private CollisionShape2D parryShape; 
    private bool isParryKnockback = false;
    private float parryKnockbackForce = 800f;
    [Export] private float parry_knockback_duration = 0.5f;
    private float parry_knockback_elapsed = 0f;
    [Export] private Inventory inventory;
    private InventoryGui inventory_ui;
    public override void _Ready()
    {
        screenSize = GetViewportRect().Size;

        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");

        zoneAtkArea = GetNode<Area2D>("ZoneAtk");
        zoneRifleAtkArea = GetNode<Area2D>("RifleAtk");

        zone_atk = zoneAtkArea.GetNode<CollisionShape2D>("CollisionShape2D");
        zoneAtkSprite = zoneAtkArea.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        zone_atk_rifle = (CollisionShape2D)GetNode("RifleAtk/RifleCollision");
        zoneRifleSprite = zoneRifleAtkArea.GetNode<AnimatedSprite2D>("RifleAnimation");

        invincibilityTimer = GetNode<Timer>("InvincibilityTimer");
        invincibilityTimer.Timeout += OnInvincibilityTimeout;

        zoneAtkArea.Monitoring = true;
        zoneAtkArea.Monitorable = true;
        zoneAtkSprite.Visible = false;

        zoneRifleAtkArea.Monitoring = true;
        zoneRifleAtkArea.Monitorable = true;
        zoneRifleSprite.Visible = true;

        zone_get_rifle = GetNode<Area2D>("../rifleGet");
        zone_get_rifle.BodyEntered += rifle_get;

        CollisionLayer = 1 << 1; // couche 2 : joueur
        CollisionMask = 1 << 0;  // couche 1 : sol

        deadScreen = GetNode<DeadScreen>("../DeadScreen");
        
        inventory_ui = GetNode<InventoryGui>("../CanvasLayer/InventoryGui");

        currentHealth = maxHealth;
        healthBar = GetNode<HealthBar>("../CanvasLayer/HealthBar");
        healthBar.SetMaxHearts(maxHealth);
        healthBar.UpdateHearts(currentHealth);

        reloadTimer = new Timer();
        AddChild(reloadTimer);
        reloadTimer.OneShot = true;
        reloadTimer.WaitTime = 2.0f; // 2 secondes pour recharger
        reloadTimer.Timeout += OnReloadFinished;
        
        parryArea = GetNode<Area2D>("Parry");
        parryShape = parryArea.GetNode<CollisionShape2D>("CollisionShape2D");
        parryArea.Monitoring = true;
        parryArea.Monitorable = true;
        
    }   


    public override void _Process(double delta)
    {
        if (isDead){
            deadScreen.death_screen();
            return;
        }
        if (isKnockback)
        {
            HandleGravity(delta);
            return;
        }
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

        if (!isAttacking)
            UpdateAttackDirection();
        if(!inventory_ui.Visible){
            Sauter();
            HandleAttack();
            Rifle(); 
            HandleParry();
            dropAll();
        }
        HandleMovement(delta);
        HandleGravity(delta);
        inventory_ui.toggle_inventory_gui();
    }

    private void Sauter(){
        if (Input.IsActionJustPressed("jump")&& jumpCount < maxJumps) {
            isJumping = true;
            jump_elapsed = 0f;
            jumpCount++;
        }
    }
    private void HandleMovement(double delta)
    {
        if (isParryKnockback)
        {
            parry_knockback_elapsed += (float)delta;
            if (parry_knockback_elapsed < parry_knockback_duration)
            {
                velocity.X = Mathf.Lerp(velocity.X, parryKnockbackForce, 1.0f);
            }
            else
            {
                isParryKnockback = false;
            }
        }
        if (isKnockback || isParryKnockback) return;
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
            if (LookingLeft)
                animatedSprite.Play("gauche");
            else
                animatedSprite.Play("droite");
        }
        else
        {
            animatedSprite.Stop();
        }

        if (!isAttacking)
 
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
            jumpCount = 0;
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

    private void UpdateAttackDirection()
    {
        isDownwardAttack=false;
        Vector2 atkOffset;
        GetNode<Area2D>("RifleAtk").RotationDegrees = 0;
        float spriteRotation = 0f;
        bool flipH = false;
        if (Input.IsActionPressed("z"))
        {
            currentAttackDirection = AttackDirection.Up;
            spriteRotation = -Mathf.Pi / 2; 
        }
        else if (Input.IsActionPressed("s") && !IsOnFloor())
        {
            currentAttackDirection = AttackDirection.Down;
            spriteRotation = Mathf.Pi / 2; 
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
                GetNode<Area2D>("RifleAtk").Position = new Vector2(10, -30);
                GetNode<Area2D>("RifleAtk").RotationDegrees = -90;
                break;

            case AttackDirection.Down:
                atkOffset = new Vector2(5, 165);
                isDownwardAttack = true;
                GetNode<Area2D>("RifleAtk").Position = new Vector2(-13, 40);
                GetNode<Area2D>("RifleAtk").RotationDegrees = 90;
                break;

            case AttackDirection.Left:
                atkOffset = new Vector2(-70, 60);
                flipH = true;
                GetNode<Area2D>("RifleAtk").Position = new Vector2(-1200, -20);
                break;

            case AttackDirection.Right:
            default:
                atkOffset = new Vector2(85, 60);
                flipH = false;
                GetNode<Area2D>("RifleAtk").Position = new Vector2(0, -20);
                break;
        }

        var shape = zone_atk.Shape as RectangleShape2D;
        if (shape != null)
        {
            switch (currentAttackDirection)
            {
                case AttackDirection.Up:
                case AttackDirection.Down:
                    shape.Size = new Vector2(90, 60);
                    break;

                case AttackDirection.Left:
                case AttackDirection.Right:
                default:
                    shape.Size = new Vector2(60, 90);
                    break;
            }
        }
        zoneAtkArea.GlobalPosition = GlobalPosition + atkOffset;
        zoneAtkSprite.Rotation = spriteRotation;
        zoneAtkSprite.FlipH = flipH;
        zoneAtkSprite.FlipV = false;
    }
    private void HandleAttack()
    {
        if (isKnockback|| isParryKnockback) return;
        if (Input.IsActionJustPressed("atk") && !isAttacking)
        {
            isAttacking = true;
            UpdateAttackDirection();
            animatedSprite.FlipH = (currentAttackDirection == AttackDirection.Left);
            zoneAtkSprite.Visible = true;
            zoneAtkSprite.Play("default");

            // Capture les ennemis présents au moment de l'attaque
            var initialTargets = new Godot.Collections.Array<Node>();
            foreach (var body in zoneAtkArea.GetOverlappingBodies())
            {
                if (body is Enemy1 || body is Enemy2)
                    initialTargets.Add(body);
            }

            foreach (var body in initialTargets)
            {
                if (body is Enemy1 enemy1)
                {
                    enemy1.TakeDamage(2);
                    if (isDownwardAttack)
                    {
                        jump_elapsed = 0f;
                        pogoJumped = true;
                    }
                }
                else if (body is Enemy2 enemy2)
                {
                    GD.Print("Enemy2 touché !");
                    enemy2.TakeDamage(2);
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
                isAttacking = false;
                zoneAtkSprite.Stop();
                zoneAtkSprite.Frame = 0;
                zoneAtkSprite.Visible = false;
            };
        }
    }

    private void Rifle(){
        GetNode<AnimatedSprite2D>("RifleAtk/RifleAnimation").Visible = false;
        if (isReloading || isKnockback || isAttacking || !hasGun || isParryKnockback)
            return;
        if(Input.IsActionJustPressed("atk_sec")){
            bulletsFired++;
            UpdateAttackDirection();
            GetNode<AnimatedSprite2D>("RifleAtk/RifleAnimation").Visible = true;
            // Capture les ennemis présents au moment de l'attaque
            var initialTargets = new Godot.Collections.Array<Node>();
            foreach (var body in zoneRifleAtkArea.GetOverlappingBodies())
            {
                if (body is Enemy1 || body is Enemy2)
                    initialTargets.Add(body);
            }

            foreach (var body in initialTargets)
            {
                if (body is Enemy1 enemy1)
                {
                    enemy1.TakeDamage(1);
                }
                else if (body is Enemy2 enemy2)
                {
                    GD.Print("Enemy2 touché !");
                    enemy2.TakeDamage(1);
                }
            }
            if (bulletsFired >= maxBullets)
            {
                isReloading = true;
                reloadTimer.Start();
            }
            var disableTimer = GetTree().CreateTimer(0.3f);
            disableTimer.Timeout += () =>
            {
                isAttacking = false;
                zoneRifleSprite.Visible = false;
            };
        }
    }


    private void HandleParry()
    {
        if (Input.IsActionJustPressed("parry"))
        {
            GD.Print("Parry activée !");

            Vector2 offset = LookingLeft ? new Vector2(-70, 60) : new Vector2(85, 60);
            parryArea.GlobalPosition = GlobalPosition + offset;

            // Vérifie s'il y a un ennemi dans la zone
            foreach (var body in parryArea.GetOverlappingBodies())
            {
                GD.Print("Détecté dans parry : " + body.Name);
                if (body is Enemy1 || body is Enemy2)
                {
                    GD.Print("Parry réussie !");
                    isParryKnockback = true;
                    parry_knockback_elapsed = 0f;
                    parryKnockbackForce = LookingLeft ? 800f : -800f;
                    return;
                }
            }
        }
    }

    private void rifle_get(Node body){
        if(body == this){
            hasGun = true;
            GetNode<CollisionShape2D>("../rifleGet/rifleGetCollision").CallDeferred("set_disabled", true);
            GetNode<Sprite2D>("../rifleGet/rifleGetCollision/rifleGetSprite").Visible = false;
        }
    }
    

    private void dropAll(){
        if(Input.IsActionJustPressed("drop")){
            hasGun = false;
            GetNode<CollisionShape2D>("../rifleGet/rifleGetCollision").CallDeferred("set_disabled", false);
            GetNode<Sprite2D>("../rifleGet/rifleGetCollision/rifleGetSprite").Visible = true;
        }
    }

    public bool IsInvincible() => isHitBoxTriggered;

    public void TakeDamage(int amount)
    {
        if (isHitBoxTriggered || isKnockback || isParryKnockback)
            return;

        isHitBoxTriggered = true;
        isKnockback = true;
        knockbackJumped = true;
        knockback_elapsed = 0f;

        isJumping = false;
        jump_elapsed = 0f;

        CollisionMask &= ~2u;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        healthBar.UpdateHearts(currentHealth);

        GD.Print($"Vie restante du joueur : {currentHealth}");

        // Knockback horizontal
        velocity.X = LookingLeft ? knockback_horizontal_force : -knockback_horizontal_force;

        if (currentHealth <= 0){
            isDead = true;
            deadScreen.death_screen();
            var deathTimer = GetTree().CreateTimer(1.0); 
            deathTimer.Timeout += () => {
                CallDeferred("queue_free");
            };
        }

    }

    private void OnInvincibilityTimeout()
    {
        isHitBoxTriggered = false;
        CollisionMask |= 2u;
    }

    private void OnReloadFinished()
    {
        bulletsFired = 0;
        isReloading = false;
    }
}