using Godot;
using System;

public partial class hotbarSlot : Button
{
    private Sprite2D background_sprite;
    private ItemStackGui item_stack_gui;
    public override void _Ready()
    {
        background_sprite = GetNode<Sprite2D>("background");
        item_stack_gui = GetNode<ItemStackGui>("CenterContainer/Panel");
        if (item_stack_gui == null)
            GD.PrintErr($"[hotbarSlot] pas trouvé ItemStackGui dans {Name}");
    }

    public void update_to_slot(InventorySlot slot){
        if(slot == null || slot.Item == null || item_stack_gui == null){
            item_stack_gui.Visible = false;
            return;
        }
        item_stack_gui.slot = slot;
        item_stack_gui.UpdateItems(slot);
        item_stack_gui.Visible = true;
    }
}
