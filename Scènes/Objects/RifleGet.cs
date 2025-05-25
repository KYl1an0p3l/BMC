using Godot;
using System;


public partial class RifleGet : Area2D
{  
    [Export] private InventoryItems itemRes;
    [Export] private Inventory inv;
    public bool hasGun = false;

    public override void _Ready()
    {
        this.BodyEntered += rifle_get;
    }

    public void rifle_get(Node body){
        if(body.Name == "PP"){
            hasGun = true;
            GetNode<CollisionShape2D>("rifleGetCollision").CallDeferred("set_disabled", true);
            GetNode<Sprite2D>("rifleGetCollision/rifleGetSprite").Visible = false;
            if (inv == null)
            {
                GD.PrintErr("Inventory est null dans RifleGet !");
                return;
            }
            inv.Insert(itemRes);

        }
    }
}
