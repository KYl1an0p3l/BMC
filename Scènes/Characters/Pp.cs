using Godot;
using System;

public partial class Pp : CharacterBody2D
{
    [Export] private int SPEED = 200;
    private float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    [Export] private int MAX_FALL_SPEED = 30;

    private AnimatedSprite2D animatedSprite;
    private CollisionShape2D collisionShape2D;
    private Vector2 velocity;
    private Vector2 screenSize;
    private CollisionShape2D zone_atk;
    private Area2D Test_hitBoxArea;
    private bool IsAttacking, isHitBoxTriggered = false;
    private bool LookingLeft = false;
    private int maxHealth = 3;
    private int currentHealth;
    private HBoxContainer heartsContainer;

    public override void _Ready(){
        screenSize = GetViewportRect().Size;
        animatedSprite = (AnimatedSprite2D)GetNode("AnimatedSprite2D");
        collisionShape2D = (CollisionShape2D)GetNode("CollisionShape2D");
        zone_atk = (CollisionShape2D)GetNode("ZoneAtk/CollisionShape2D");
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
        Attaque();
        

        

        
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
        if(!IsOnFloor()){
            velocity.Y += gravity;
        }
        else{
            velocity.Y = 0;
        }

        Velocity = velocity; //Car la fonction MoveAndSlide() utilise la variable Velocity et pas velocity
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
}
