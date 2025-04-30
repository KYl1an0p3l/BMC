using Godot;
using System;

public partial class Pp : CharacterBody2D
{
    [Export]
    private int SPEED = 400;

    private AnimatedSprite2D animatedSprite;
    private CollisionShape2D collisionShape2D;
    private Vector2 velocity;
    private Vector2 screenSize;
    public override void _Ready(){
        screenSize = GetViewportRect().Size;
        animatedSprite = (AnimatedSprite2D)GetNode("AnimatedSprite2D");
        collisionShape2D = (CollisionShape2D)GetNode("CollisionShape2D");
    }

    public override void _Process(double delta)
    {
        velocity = new Vector2();
        if(Input.IsActionPressed("ui_right")){
            velocity.X++;
        }
        if(Input.IsActionPressed("ui_left")){
            velocity.X--;
        }
        if(velocity.Length() > 0){
            velocity = velocity.Normalized() * SPEED;
            animatedSprite.Play();
        }
        else{
            animatedSprite.Stop();
        }

        Position += velocity * (float)delta;
        Position = new Vector2(
            Mathf.Clamp(Position.X, 0, screenSize.X), 
            Mathf.Clamp(Position.Y, 0, screenSize.Y)
        );

        if(velocity.X != 0){
            animatedSprite.Animation = "gauche";
            animatedSprite.FlipH = velocity.X > 0;
            animatedSprite.FlipV = false;
        }
    }

}
