using Godot;
using System;

public partial class Pp : CharacterBody2D
{
    [Export] private int SPEED = 200;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    [Export] private int MAX_FALL_SPEED = 50;

    [Export] private float pogo_jump_power = -670f;
    [Export] private float pogo_jump_duration = 0.25f; 
    [Export] private float max_jump_hold_time = 0.25f;
    [Export] private float jump_time = 0.25f; // durée de jump_power 
    [Export] private float jump_power = -970f; // force du saut
    private float jump_elapsed = 0f;// chronomètre
    private bool isJumping = false;


    private AnimatedSprite2D animatedSprite;
    private CollisionShape2D collisionShape2D;
    private Vector2 velocity;
    private Vector2 screenSize;
    private CollisionShape2D zone_atk, zone_atk_rifle, rifleGetDisable;
    private Area2D Test_hitBoxArea, zone_get_rifle;
    private bool IsAttacking, isHitBoxTriggered, hasGun, isDead, isDownwardAttack = false;
    private bool LookingLeft = false;
    private DeadScreen deadScreen;
    private int maxHealth = 3;
    private int currentHealth;
    private HBoxContainer heartsContainer;


    private Timer invincibilityTimer;
    [Export] private float knockback_jump_power = -500f;
    [Export] private float knockback_horizontal_force = 250f;
    private bool isKnockback = false;
    private bool knockbackJumped = false;
    private float knockback_elapsed = 0f;
    [Export] private float knockback_duration = 0.25f;
    private bool pogoJumped = false;

    public override void _Ready(){
        screenSize = GetViewportRect().Size;

        animatedSprite = (AnimatedSprite2D)GetNode("AnimatedSprite2D");
        collisionShape2D = (CollisionShape2D)GetNode("CollisionShape2D");

        zone_atk = (CollisionShape2D)GetNode("ZoneAtk/CollisionShape2D");

        invincibilityTimer = GetNode<Timer>("InvincibilityTimer");
        invincibilityTimer.Timeout += OnInvincibilityTimeout;

        CollisionLayer = 1 << 1; // couche 2 : joueur
        CollisionMask = 1 << 0;  // couche 1 : sol

        zone_atk_rifle = (CollisionShape2D)GetNode("RifleAtk/RifleCollision");
        zone_get_rifle = GetNode<Area2D>("../../rifleGet");
        zone_get_rifle.BodyEntered += rifle_get;

        Test_hitBoxArea = GetNode<Area2D>("../../HurtBox/hitBox");
        Test_hitBoxArea.BodyEntered += OnHitBoxBodyEntered;

        currentHealth = maxHealth;
        heartsContainer = GetNode<HealthBar>("../../CanvasLayer/HealthBar");
        ((HealthBar)heartsContainer).UpdateHearts(currentHealth);

        deadScreen = GetNode<DeadScreen>("../../deadScreen");
        
        
    }

    public override void _Process(double delta)
    {
        if (isDead){ //Si le joueur est mort, on ne peut pas continuer à jouer en arrière-plan
            return;
        }
        else if (isKnockback)
        {
            HandleGravity(delta);
            return;
        }
        velocity = new Vector2();

        HandleMovement(delta);
        HandleGravity(delta);
        Sauter();
        Attaque();
        Rifle();
        dropAll();
        

        

        
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

        if (!IsAttacking)
            animatedSprite.FlipH = velocity.X > 0;


        Position += velocity * (float)delta;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 0, screenSize.X),
            Mathf.Clamp(Position.Y, 0, screenSize.Y)
        );
    }

        private void HandleGravity(double delta){
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
    private void Attaque(){
        zone_atk.SetDisabled(true);
        GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = false;
        if(Input.IsActionJustPressed("atk") && !IsAttacking){ //Lorsqu'on attaque
            IsAttacking = true;
            zone_atk.SetDisabled(false);
            GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = true;

            bool up = Input.IsActionPressed("z");
            bool left = Input.IsActionPressed("q");
            bool right = Input.IsActionPressed("d");
            bool down = Input.IsActionPressed("s");

            if (up) {
                // attaque vers le haut
                GetNode<Area2D>("ZoneAtk").Position = new Vector2(0, -30);
                isDownwardAttack=false;
            }
            else if(down && !IsOnFloor()){
                GetNode<Area2D>("ZoneAtk").Position = new Vector2(0, 165);
                isDownwardAttack=true;
            }
            else if (left) {
                // attaque à gauche
                GetNode<Area2D>("ZoneAtk").Position = new Vector2(-85, 60);
                isDownwardAttack=false;
            }
            else if (right) {
                // attaque à droite
                GetNode<Area2D>("ZoneAtk").Position = new Vector2(85, 60);
                isDownwardAttack=false;
            }
            else {
                // attaque de base selon direction
                GetNode<Area2D>("ZoneAtk").Position = LookingLeft ? new Vector2(-85, 60) : new Vector2(85, 60);
                isDownwardAttack=false;
            }

            // Capture les ennemis présents au moment de l'attaque
            var initialTargets = new Godot.Collections.Array<Node>();
            foreach (var body in GetNode<Area2D>("ZoneAtk").GetOverlappingBodies())
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
            var disableTimer = GetTree().CreateTimer(0.6f);
            disableTimer.Timeout += () =>
            {
                IsAttacking = false;
                GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = false;
            };
        }
    }

    public bool IsInvincible() => isHitBoxTriggered;

    private void Rifle(){
        zone_atk_rifle.SetDisabled(true);
        GetNode<AnimatedSprite2D>("RifleAtk/RifleAnimation").Visible = false;
        GetNode<Area2D>("RifleAtk").RotationDegrees = 0; //On reset la rotation à 0
        if(Input.IsActionJustPressed("atk_sec") && hasGun){ //Lorsqu'on attaque
            zone_atk_rifle.SetDisabled(false);
            GetNode<AnimatedSprite2D>("RifleAtk/RifleAnimation").Visible = true;
            bool up = Input.IsActionPressed("z");
            bool left = Input.IsActionPressed("q");
            bool right = Input.IsActionPressed("d");
            bool down = Input.IsActionPressed("s");
            if (up) {
                // attaque vers le haut
                GetNode<Area2D>("RifleAtk").Position = new Vector2(10, -30);
                GetNode<Area2D>("RifleAtk").RotationDegrees = -90;
            }
            else if(down){
                GetNode<Area2D>("RifleAtk").Position = new Vector2(-13, 40);
                GetNode<Area2D>("RifleAtk").RotationDegrees = 90;
            }
            else if (left) {
                // attaque à gauche
                GetNode<Area2D>("RifleAtk").Position = new Vector2(-1200, 20);
            }
            else if (right) {
                // attaque à droite
                GetNode<Area2D>("RifleAtk").Position = new Vector2(20, 20);
            }
            else {
                // attaque de base selon direction
                GetNode<Area2D>("RifleAtk").Position = LookingLeft ? new Vector2(-1200, 20) : new Vector2(20, 20);
            }
            
        }
    }

    private void OnHitBoxBodyEntered(Node body){
    if (body == this)
        {
            if(currentHealth <= 1){
                currentHealth -= 1;
                ((HealthBar)heartsContainer).UpdateHearts(currentHealth);
                deadScreen.death_screen();
                isDead = true;
                QueueFree();
            }
            else{
                currentHealth -= 1;
                ((HealthBar)heartsContainer).UpdateHearts(currentHealth);
            }
            GD.Print("Vie restante : " + currentHealth);
        }

    }
    private void Sauter(){
        if (IsOnFloor() && Input.IsActionJustPressed("ui_accept")) {
            isJumping = true;
            jump_elapsed = 0f;
        }
    }

    private void dropAll(){
        if(Input.IsActionJustPressed("ui_up")){
            hasGun = false;
            GetNode<CollisionShape2D>("../../rifleGet/rifleGetCollision").CallDeferred("set_disabled", false);
            GetNode<Sprite2D>("../../rifleGet/rifleGetCollision/rifleGetSprite").Visible = true;
        }
    }

    private void rifle_get(Node body){
        if(body == this){
            hasGun = true;
            GetNode<CollisionShape2D>("../../rifleGet/rifleGetCollision").CallDeferred("set_disabled", true);
            GetNode<Sprite2D>("../../rifleGet/rifleGetCollision/rifleGetSprite").Visible = false;
        }
    }

    private void OnInvincibilityTimeout()
    {
        isHitBoxTriggered = false;
        CollisionMask |= 2u;
    }

    public void TakeDamage()
    {
        if (isHitBoxTriggered || isKnockback)
            return;

        isHitBoxTriggered = true;
        isKnockback = true;
        knockbackJumped = true;
        knockback_elapsed = 0f;

        isJumping = false;
        jump_elapsed = 0f;

        CollisionMask &= ~2u;

        // Knockback horizontal
        velocity.X = LookingLeft ? knockback_horizontal_force : -knockback_horizontal_force;
    }
}
