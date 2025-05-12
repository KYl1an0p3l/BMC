using Godot;
using System;

public partial class Slot : Panel
{
    Sprite2D backgroundSprite, itemSprite;
    public override void _Ready()
    {
        backgroundSprite = GetNode<Sprite2D>("background");
        itemSprite = GetNode<Sprite2D>("CenterContainer/Panel/item");
    }

    public void UpdateItems(InventoryItems item){
        if(item == null){
            //backgroundSprite.Frame = 0; si plus tard on veut une texture alternative
            itemSprite.Visible = false;
        }
        else{
            //backgroundSprite.Frame = 1; si plus tard on veut une texture alternative
            itemSprite.Visible = true;
            itemSprite.Texture = item.Texture;
        }
    }
}
