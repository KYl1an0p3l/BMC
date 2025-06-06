using Godot;
using System;
using System.Linq.Expressions;

public partial class Pp : CharacterBody2D
{
    [Export] private int SPEED = 200;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    [Export] private int MAX_FALL_SPEED = 50;
    private PackedScene rifleProjectileScene;

    private AnimatedSprite2D animatedSprite;
    private CollisionShape2D collisionShape2D;

    private Area2D zoneAtkArea, zone_get_rifle;
    
    private CollisionShape2D zone_atk, rifleGetDisable;
    private enum AttackDirection { Left, Right, Up, Down }
    private AttackDirection currentAttackDirection = AttackDirection.Right;
    private AttackDirection lastHorizontalDirection = AttackDirection.Right;
    private bool isDownwardAttack = false;
    private bool isUpwardAttack = false;
    private bool isAttacking = false;

    private bool isShooting = false;    
    
    [Export] private float pogo_jump_power = -670f;
    [Export] private float pogo_jump_duration = 0.3f; 
    [Export] private float jump_power = -770f; // force 
    private float jump_elapsed = 0f;// chronomètre
    private bool isJumping = false;
    [Export] private float max_jump_hold_time = 0.3f;//durée max d'apui bouton
    private bool pogoJumped = false;
    private int jumpCount = 0;
    [Export] private int maxJumps = 2;

    private int healsalle = 3; 
    private float hKeyHoldTime = 0f;
    private float healInterval = 1f;   //heal
    private float healTimer = 0f;
    private bool isHealingActive = false;
    private bool bloquer_actions=false;

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

    [Export] public int maxHealth = 3;
    public int currentHealth;
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

    [Export] private float KB_hit_force = 250f;
    private bool is_KB_hit = false;
    [Export] private float KB_hit_duration = 0.1f;
    private float KB_hit_elapsed = 0f;

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
    private Inventory inventory;
    private InventoryGui inventory_ui;
    InventoryItems ScythObj, RifleObj;

    public override void _Ready()
    {
        isDead = false;

        inventory = GameState.Instance.PlayerInventory;
        RichTextLabel richText = GetNode<RichTextLabel>("UI/heal");
        richText.ParseBbcode("[color=orange][font_size=24]HEALS LEFT :[/font_size][/color] [color=white][font_size=24]" + healsalle.ToString() + "[/font_size][/color]");

        var reloadSprite = GetNode<AnimatedSprite2D>("ReloadSprite");
        reloadSprite.Stop();                // Arrête l'animation
        reloadSprite.Visible = false;       // Cache le sprite

        screenSize = GetViewportRect().Size;
        AddToGroup("player");

        animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");

        zoneAtkArea = GetNode<Area2D>("ZoneAtk");

        zone_atk = zoneAtkArea.GetNode<CollisionShape2D>("CollisionShape2D");

        invincibilityTimer = GetNode<Timer>("InvincibilityTimer");
        invincibilityTimer.Timeout += OnInvincibilityTimeout;

        ScythObj = GD.Load<InventoryItems>("res://Inventory/Faux.tres");
        RifleObj = GD.Load<InventoryItems>("res://Inventory/Gun.tres");
        rifleProjectileScene = GD.Load<PackedScene>("res://Scènes/Objects/rifle_projectile.tscn");

        zoneAtkArea.Monitoring = true;
        zoneAtkArea.Monitorable = true;
        if (GetParent().Name == "DevTestMap")
        {
            zone_get_rifle = GetNode<Area2D>("../rifleGet");
        }
        CollisionLayer = 1 << 1; // couche 2 : joueur
        CollisionMask = 1 << 0;  // couche 1 : sol

        deadScreen = GetNode<DeadScreen>("CanvasLayer/DeadScreen");

        inventory_ui = GetNode<InventoryGui>("CanvasLayer/InventoryGui");


        currentHealth = maxHealth;
        healthBar = GetNode<HealthBar>("CanvasLayer/HealthBar");
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
        this.Name = "PP";
        // Réattache le CanvasLayer si besoin
        var canvasLayer = GetTree().Root.GetNodeOrNull<CanvasLayer>("CanvasLayer");
        if (canvasLayer != null && canvasLayer.GetParent() != this)
        {
            GetTree().Root.RemoveChild(canvasLayer);
            AddChild(canvasLayer);
        }

        deadScreen.HideDeathScreen();


        // Mise à jour de l'inventaire si tout est prêt
        inventory_ui?.UpdateInventory();
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

        if (isDashing && !bloquer_actions)
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
        else if (Input.IsActionJustPressed("F") && dashCooldownTimer <= 0 && !bloquer_actions)//touche f 
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
        if (Input.IsActionPressed("H") && IsOnFloor() && healsalle >= 1 && currentHealth <= maxHealth - 1){

            hKeyHoldTime += (float)delta;
            bloquer_actions=true;
            if (hKeyHoldTime >= 1.0f)
            {
                isHealingActive = true;
                healTimer += (float)delta;
                if (healTimer >= healInterval){


                    currentHealth = Mathf.Min(currentHealth + 1, maxHealth);
                    healsalle -= 1;
                    GetNode<RichTextLabel>("UI/heal").ParseBbcode("[color=orange][font_size=24]HEALS LEFT : [/font_size][/color] [color=white][font_size=24]" + healsalle.ToString() + "[/font_size][/color]");
                    GD.Print("vie_salle: ", healsalle);
                    healthBar.UpdateHearts(currentHealth);
                    healTimer = 0f;

                    if (healsalle < 1 || currentHealth >= maxHealth)
                    {
                        isHealingActive = false;
                        bloquer_actions=false;
                    }
                }
            }
        }
        else {


            hKeyHoldTime = 0f;
            healTimer = 0f;
            isHealingActive = false;
            bloquer_actions=false;



        }

        velocity = new Vector2();

        if (!isAttacking || !isShooting)
            UpdateAttackDirection();
        if(!inventory_ui.Visible){
            Sauter();
            HandleAttack();
            Rifle(); 
            HandleParry();
        }
        HandleMovement(delta);
        HandleGravity(delta);
        inventory_ui.toggle_inventory_gui();
    }

    private void Sauter(){
        if (Input.IsActionJustPressed("jump")&& jumpCount < maxJumps && !bloquer_actions) {
            isJumping = true;
            jump_elapsed = 0f;
            jumpCount++;
        }
    }
    private void HandleMovement(double delta)
    {
        bool isUnderForcedMovement = false;

        if (isParryKnockback)
        {
            parry_knockback_elapsed += (float)delta;
            if (parry_knockback_elapsed < parry_knockback_duration)
            {
                velocity.X = Mathf.Lerp(velocity.X, parryKnockbackForce, 1.0f);
                isUnderForcedMovement = true;
            }
            else
            {
                isParryKnockback = false;
            }
        }

        if (is_KB_hit)
        {
            KB_hit_elapsed += (float)delta;
            if (KB_hit_elapsed < KB_hit_duration)
            {
                velocity.X = Mathf.Lerp(velocity.X, KB_hit_force, 1.0f);
                isUnderForcedMovement = true;
            }
            else
            {
                is_KB_hit = false;
            }
        }

        // Si en knockback (ou parry), ne traiter que l'animation mais PAS les inputs
        if (!isUnderForcedMovement && !isKnockback && !bloquer_actions)
        {
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
            }
        }

// ANIMATION (toujours traitée)
        if (velocity.Length() > 0)
        {
            if (bloquer_actions && !isHealingActive)
            {
                animatedSprite.Play(LookingLeft ? "charge_heal_left" : "charge_heal_right");
            }
            else if (isHealingActive)
            {
                animatedSprite.Play(LookingLeft ? "heal_left" : "heal_right");
            }
            if (isDashing)
            {
                animatedSprite.Play(LookingLeft ? "dash_left" : "dash_right");
            }
            else if (!IsOnFloor() && !isDashing && !isDownwardAttack && !isUpwardAttack)
            {
                animatedSprite.Play(LookingLeft ? "jump_left" : "jump_right");
            }
            else if (isAttacking)
            {
                if (isDownwardAttack)
                    animatedSprite.Play(LookingLeft ? "down_atk_left" : "down_atk_right");
                else if (isUpwardAttack)
                    animatedSprite.Play(LookingLeft ? "up_atk_left" : "up_atk_right");
                else
                    animatedSprite.Play(LookingLeft ? "atk_left" : "atk_right");
            }
            else if (isShooting)
            {
                animatedSprite.Play(LookingLeft ? "rifle_left" : "rifle_right");
            }
            else
            {
                animatedSprite.Play(LookingLeft ? "gauche" : "droite");
            }
        }
        else if (velocity.Length() == 0)
        {
            if (bloquer_actions && !isHealingActive)
            {
                animatedSprite.Play(LookingLeft ? "charge_heal_left" : "charge_heal_right");
            }
            else if (isHealingActive)
            {
                animatedSprite.Play(LookingLeft ? "heal_left" : "heal_right");
            }
            else if (isDashing)
            {
                animatedSprite.Play(LookingLeft ? "dash_left" : "dash_right");
            }
            else if (!IsOnFloor() && !isDashing && !isDownwardAttack && !isUpwardAttack)
            {
                animatedSprite.Play(LookingLeft ? "jump_left" : "jump_right");
            }
            else if (isAttacking)
            {
                if (isDownwardAttack)
                    animatedSprite.Play(LookingLeft ? "down_atk_left" : "down_atk_right");
                else if (isUpwardAttack)
                    animatedSprite.Play(LookingLeft ? "up_atk_left" : "up_atk_right");
                else
                    animatedSprite.Play(LookingLeft ? "atk_left" : "atk_right");
            }
            else if (isShooting)
            {
                animatedSprite.Play(LookingLeft ? "rifle_left" : "rifle_right");
            }
            else
            {
                animatedSprite.Play(LookingLeft ? "idle_left" : "idle_right");
            }
        }

        // Application du mouvement
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
            velocity.Y += (gravity / 1.55f);
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
        isUpwardAttack = false;
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
                isUpwardAttack = true;
                atkOffset = new Vector2(5, -30);
                break;

            case AttackDirection.Down:
                atkOffset = new Vector2(5, 165);
                isDownwardAttack = true;
                break;

            case AttackDirection.Left:
                atkOffset = new Vector2(-20, 60);
                break;

            case AttackDirection.Right:
            default:
                atkOffset = new Vector2(85, 60);
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
                    shape.Size = new Vector2(40, 90);
                    break;
            }
        }
        zoneAtkArea.GlobalPosition = GlobalPosition + atkOffset;
    }
    private void HandleAttack()
    {
        if (bloquer_actions || isKnockback|| isParryKnockback || string.IsNullOrWhiteSpace(ScythObj?.ActionName)) return;
        else if (Input.IsActionJustPressed(ScythObj.ActionName) && !isAttacking && !isShooting)
        {
            isAttacking = true;
            UpdateAttackDirection();

            // Capture les ennemis présents au moment de l'attaque
            var initialTargets = new Godot.Collections.Array<Node>();
            foreach (var body in zoneAtkArea.GetOverlappingBodies())
            {
                if (body is Enemy1 || body is Enemy2 || body is BossTest)
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
                    else{
                        is_KB_hit = true;
                        KB_hit_elapsed = 0f;
                        KB_hit_force = LookingLeft ? 550f : -550f;
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
                    else{
                        is_KB_hit = true;
                        KB_hit_elapsed = 0f;
                        KB_hit_force = LookingLeft ? 550f : -550f;
                    
                    }
                }
                else if (body is BossTest boss)
                {
                    GD.Print("Enemy2 touché !");
                    boss.TakeDamage(2);
                    if (isDownwardAttack)
                    {
                        jump_elapsed = 0f;
                        pogoJumped = true;
                    }
                    else{
                        is_KB_hit = true;
                        KB_hit_elapsed = 0f;
                        KB_hit_force = LookingLeft ? 550f : -550f;
                    
                    }
                }
            }


            // Timer pour désactiver la zone d'attaque et réactiver l'attaque
            var disableTimer = GetTree().CreateTimer(0.5f);
            disableTimer.Timeout += () =>
            {
                isAttacking = false;
                animatedSprite.Stop();
                animatedSprite.Frame = 0;
            };
        }
    }

    private void Rifle()
    {
        if (bloquer_actions || isReloading || isKnockback || isAttacking || isShooting || string.IsNullOrWhiteSpace(RifleObj?.ActionName))
            return;

        if (Input.IsActionJustPressed(RifleObj.ActionName))
        {
            isShooting = true;
            bulletsFired++;
            UpdateAttackDirection();

            // Instanciation du projectile
            var projectile = (RifleProjectile)rifleProjectileScene.Instantiate();

            // Définir la direction du tir
            projectile.Direction = currentAttackDirection switch
            {
                AttackDirection.Up => Vector2.Up,
                AttackDirection.Down => Vector2.Down,
                AttackDirection.Left => Vector2.Left,
                AttackDirection.Right => Vector2.Right,
                _ => Vector2.Right
            };

            // Définir la position de spawn du projectile
            Vector2 spawnOffset = currentAttackDirection switch
            {
                AttackDirection.Up => new Vector2(10, -30),
                AttackDirection.Down => new Vector2(-13, 40),
                AttackDirection.Left => new Vector2(-60, 20),
                AttackDirection.Right => new Vector2(40, 20),
                _ => Vector2.Zero
            };

            projectile.GlobalPosition = GlobalPosition + spawnOffset;

            // Ajouter à la scène
            GetParent().AddChild(projectile);
            

            // Gestion du rechargement
            if (bulletsFired >= maxBullets)
            {
                isReloading = true;
                var reloadSprite = GetNode<AnimatedSprite2D>("ReloadSprite");
                reloadSprite.Visible = true;
                reloadSprite.Play("reload");
                reloadTimer.Start();
            }

            // Timer pour réinitialiser l'état de tir
            var disableTimer = GetTree().CreateTimer(0.3f);
            disableTimer.Timeout += () =>
            {
                isShooting = false;
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
            // Détache le CanvasLayer contenant DeadScreen avant de supprimer Pp
            var canvasLayer = GetNode<CanvasLayer>("CanvasLayer"); // ou le nom exact du CanvasLayer
            if (canvasLayer.GetParent() == this)
            {
                RemoveChild(canvasLayer);
                GetTree().Root.AddChild(canvasLayer); // ou GetTree().CurrentScene si tu veux le rattacher à la scène active
            }
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
        var reloadSprite = GetNode<AnimatedSprite2D>("ReloadSprite");
        reloadSprite.Stop();                // Arrête l'animation
        reloadSprite.Visible = false;       // Cache le sprite
    }
}