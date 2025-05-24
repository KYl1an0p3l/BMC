using Godot;
using System;

public partial class hotbar : Panel
{
    Inventory inventory;
    private Godot.Collections.Array<hotbarSlot> slots = new();
    private InventorySlot inventory_slot;
    public override void _Ready()
    {
        inventory = GameState.Instance.PlayerInventory;
        foreach (var child in GetChildren()){//Encore un fois obligé de les recast, limite du C#
            if(child is hotbarSlot slot){
                slots.Add(slot);
            }
        }
        update();
        inventory.OnUpdated += update;
        
    }

    public void update(){
        for(int i=0; i<slots.Count; i++){
            int j = inventory.Slots.Count - 1 - i;
            inventory_slot = inventory.Slots[j];
            slots[i].update_to_slot(inventory_slot);
        }
    }

}
