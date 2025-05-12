using Godot;
using System;


public partial class RifleGet : Area2D
{  
    public bool hasGun = false;

    public override void _Ready()
    {
        BodyEntered += rifle_get;
    }

    public void rifle_get(Node body){
        if(body == this){
            hasGun = true;
            GetNode<CollisionShape2D>("../../rifleGet/rifleGetCollision").CallDeferred("set_disabled", true);
            GetNode<Sprite2D>("../../rifleGet/rifleGetCollision/rifleGetSprite").Visible = false;
        }
    }
}
