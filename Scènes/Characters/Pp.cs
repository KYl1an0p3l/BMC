using Godot;
using System;

public partial class Pp : CharacterBody2D
{
    [Export] private int SPEED = 200;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    [Export] private int MAX_FALL_SPEED = 30;
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
    private bool IsAttacking, isHitBoxTriggered, hasGun = false;
    private bool LookingLeft = false;
    private int maxHealth = 3;
    private int currentHealth;
    private HBoxContainer heartsContainer;

    public override void _Ready(){
        screenSize = GetViewportRect().Size;
        animatedSprite = (AnimatedSprite2D)GetNode("AnimatedSprite2D");
        collisionShape2D = (CollisionShape2D)GetNode("CollisionShape2D");
        zone_atk = (CollisionShape2D)GetNode("ZoneAtk/CollisionShape2D");
        zone_atk_rifle = (CollisionShape2D)GetNode("RifleAtk/RifleCollision");
        zone_get_rifle = GetNode<Area2D>("../../rifleGet");
        zone_get_rifle.BodyEntered += rifle_get;
        Test_hitBoxArea = GetNode<Area2D>("../../HurtBox/hitBox");
        Test_hitBoxArea.BodyEntered += OnHitBoxBodyEntered;
        currentHealth = maxHealth;
        heartsContainer = GetNode<HealthBar>("../../CanvasLayer/HealthBar");
        ((HealthBar)heartsContainer).UpdateHearts(currentHealth);
        
        
    }

    public override void _Process(double delta)
    {
        velocity = new Vector2();

        Mouvements_Limits(delta);
        Marche();
        gravity_gestion(delta);
        Sauter();
        Attaque();
        Rifle();
        dropAll();
        

        

        
    }







    private void Marche(){
        if(Input.IsActionPressed("d")){
            velocity.X++;
            LookingLeft = false;
        }
        if(Input.IsActionPressed("q")){
            velocity.X--;
            LookingLeft = true;
        }
        if(velocity.Length() > 0){
            velocity = velocity.Normalized() * SPEED;
            animatedSprite.Play("gauche");
        }
        else{
            animatedSprite.Stop();
        }
        if(velocity.X != 0){
            animatedSprite.Animation = "gauche";
            animatedSprite.FlipH = velocity.X > 0;
            animatedSprite.FlipV = false;
        }
    }

    private void Mouvements_Limits(double delta){
        //Border Limits
        Position += velocity * (float)delta;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 0, screenSize.X), 
            Mathf.Clamp(Position.Y, 0, screenSize.Y)
        );
        //Gravity Speed Limits
        Mathf.Clamp(velocity.Y, -10, MAX_FALL_SPEED);
    }

       private void gravity_gestion(double delta){
        if (isJumping)
        {
            jump_elapsed += (float)delta; // delta durée entre deux frame

            if (jump_elapsed < max_jump_hold_time && Input.IsActionPressed("ui_accept"))
            {
                velocity.Y += jump_power ;
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

    private void Attaque(){
        zone_atk.SetDisabled(true);
        GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = false;
        if(Input.IsActionJustPressed("atk")){ //Lorsqu'on attaque
            zone_atk.SetDisabled(false);
            GetNode<Sprite2D>("ZoneAtk/Sprite2D").Visible = true;
            bool up = Input.IsActionPressed("z");
            bool left = Input.IsActionPressed("q");
            bool right = Input.IsActionPressed("d");
            bool down = Input.IsActionPressed("s");
            if (up) {
                // attaque vers le haut
                GetNode<Area2D>("ZoneAtk").Position = new Vector2(0, -30);
            }
            else if(down && !IsOnFloor()){
                GetNode<Area2D>("ZoneAtk").Position = new Vector2(0, 165);
            }
            else if (left) {
                // attaque à gauche
                GetNode<Area2D>("ZoneAtk").Position = new Vector2(-85, 60);
            }
            else if (right) {
                // attaque à droite
                GetNode<Area2D>("ZoneAtk").Position = new Vector2(85, 60);
            }
            else {
                // attaque de base selon direction
                GetNode<Area2D>("ZoneAtk").Position = LookingLeft ? new Vector2(-85, 60) : new Vector2(85, 60);
            }
            
        }
    }

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
            if(currentHealth <= 0){
                currentHealth = maxHealth;
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
}
