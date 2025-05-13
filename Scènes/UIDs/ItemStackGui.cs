using Godot;
using System;

public partial class ItemStackGui : Panel
{
    public InventorySlot slot;
    private Sprite2D itemSprite;
    private Label amountLabel;

    public override void _Ready()
    {
        itemSprite = GetNode<Sprite2D>("item");
        amountLabel = GetNode<Label>("Label");
    }

    public void UpdateItems(InventorySlot slot){
        if(slot == null){
            //backgroundSprite.Frame = 0; si plus tard on veut une texture alternative
            itemSprite.Visible = false;
            amountLabel.Visible = false;
        }
        else{
            //backgroundSprite.Frame = 1; si plus tard on veut une texture alternative
            itemSprite.Visible = true;
            itemSprite.Texture = slot.Item.Texture;
            amountLabel.Visible = false;
            if(slot.amount > 1){
                amountLabel.Text = slot.amount.ToString();
                amountLabel.Visible = true;
            }
        }
    }
}
