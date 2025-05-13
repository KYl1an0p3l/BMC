using Godot;
using System;
using System.ComponentModel;


public partial class InventoryGui : Control
{
    private Inventory inventory;
    private PackedScene itemStackLoad;
    private Godot.Collections.Array<Slot> slots = new();
    private ItemStackGui itemInHand;
    private int index;
    public override void _Ready()
    {
        Visible = false;
        index = 0;
        inventory = GD.Load<Inventory>("res://Inventory/playerInventory.tres");
        itemStackLoad = GD.Load<PackedScene>("res://Scènes/UIDs/itemStackGui.tscn");
        var slotNodes = GetNode<GridContainer>("NinePatchRect/GridContainer").GetChildren();
        foreach (Node node in slotNodes){//On recast les noeuds puisqu'on ne peut pas le faire automatiquement
            if (node is Slot slot){
                slot.index = index;
                slots.Add(slot);
                index++;
                slot.Pressed += () => onSlotClicked(slot);
            }
        }
        index = 0;
        inventory.OnUpdated += UpdateInventory; //reçois l'évènement
        UpdateInventory();
    }
    public void toggle_inventory_gui(){
        if(Input.IsActionJustPressed("toggle_inventory_gui")){
            Visible = !Visible;
        }
    }

    public void UpdateInventory(){
        for(int i = 0; i < Math.Min(inventory.Slots.Count, slots.Count); i++){
            InventorySlot inventorySlot = inventory.Slots[i];
            if(inventorySlot == null || inventorySlot.Item == null){
                continue;
            }

            ItemStackGui isg = slots[i].itemStack;
            if(isg == null){
                isg = itemStackLoad.Instantiate<ItemStackGui>();
                slots[i].InsertItem(isg);
            }

            if (isg != null) {
                isg.slot = inventorySlot;
                isg.UpdateItems(inventorySlot);
            }
        }
    }
    public void onSlotClicked(Slot slot){
        if(slot.isEmpty() && itemInHand != null){
            insertItemInSlot(slot);
            return;
        }
        else if(itemInHand == null){
            takeItemFromSlot(slot);
            return;
        }
        swapItems(slot);
    }

    public void UpdateItemInHand(){
        if(itemInHand == null){
            return;
        }
        else{
            itemInHand.GlobalPosition = GetGlobalMousePosition() - itemInHand.Size / 2;
        }
    }

    public override void _Input(InputEvent @event)
    {
        UpdateItemInHand();
    }

    public void takeItemFromSlot(Slot slot){
        itemInHand = slot.takeItem();
        if(itemInHand != null){
            AddChild(itemInHand);
            UpdateItemInHand();
        }
    }

    public void insertItemInSlot(Slot slot){
        var item = itemInHand;
        if(itemInHand != null){
            RemoveChild(itemInHand);
            itemInHand = null;
            slot.InsertItem(item);
        }
        
    }

    public void swapItems(Slot slot){
        var tempItem = slot.takeItem();
        insertItemInSlot(slot);
        itemInHand = tempItem;
        if(itemInHand != null){
            AddChild(itemInHand);
        }
        UpdateItemInHand();
    }
}
