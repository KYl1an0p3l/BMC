using Godot;
using System;


public partial class InventoryGui : Control
{
    private Inventory inventory;
    private Godot.Collections.Array<Slot> slots = new();
    public override void _Ready()
    {
        Visible = false;
        inventory = GD.Load<Inventory>("res://Inventory/playerInventory.tres");
        var slotNodes = GetNode<GridContainer>("NinePatchRect/GridContainer").GetChildren();
        foreach (Node node in slotNodes){//On recast les noeuds puisqu'on ne peut pas le faire automatiquement
            if (node is Slot slot)
                slots.Add(slot);
        }
        UpdateInventory();
    }

    public void toggle_inventory_gui(){
        if(Input.IsActionJustPressed("toggle_inventory_gui")){
            Visible = !Visible;
        }
    }

    public void UpdateInventory(){
        for(int i=0;i < Math.Min(inventory.Items.Count, slots.Count) ; i++){
            slots[i].UpdateItems(inventory.Items[i]);
        }
    }

}
