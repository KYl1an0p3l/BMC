using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;


public partial class InventoryGui : Control
{
    private Inventory inventory;
    private PackedScene itemStackLoad;
    private Godot.Collections.Array<Slot> slots = new();
    private ItemStackGui itemInHand;
    private int index, oldIndex;
    private bool locked = false;
    public override void _Ready()
    {
        Visible = false;
        oldIndex = -1;
        index = 0;
        inventory = GameState.Instance.PlayerInventory;
        itemStackLoad = GD.Load<PackedScene>("res://Scènes/UIDs/itemStackGui.tscn");
        var slotNodes = GetNode<Panel>("NinePatchRect/GridContainer").GetChildren();
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
            
            if(inventory == null || inventory.Slots[i] == null || inventory.Slots[i].Item == null){
                continue;
            }
            InventorySlot inventorySlot = inventory.Slots[i];
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
        if(locked){ return ;}
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

    public async Task putItemBack(){
        locked= true;
        if(oldIndex < 0){
            var emptySlots = slots.Where(s => s.isEmpty()).ToList();
            if(!emptySlots.Any())
                return;
            oldIndex = emptySlots[0].index;
        }
        var targetSlot = slots[oldIndex];

        var tween = CreateTween(); //On créer l'animation de retour de l'objet dans son slot
        var targetPosition = targetSlot.GlobalPosition + targetSlot.Size / 2;
        tween.TweenProperty(itemInHand, "global_position", targetPosition, 0.2);
        await ToSignal(tween, Tween.SignalName.Finished);

        insertItemInSlot(targetSlot);
        locked = false;
    }
    public override void _Input(InputEvent @event)
    {
        if(Input.IsActionJustPressed("rightClick") && !locked && itemInHand != null){
            putItemBack();
        }
        UpdateItemInHand();
    }

    public void takeItemFromSlot(Slot slot){
        itemInHand = slot.takeItem();
        if(itemInHand != null){
            AddChild(itemInHand);
            UpdateItemInHand();
        }
        oldIndex = slot.index;
    }

    public void insertItemInSlot(Slot slot){
        var item = itemInHand;
        if(itemInHand != null){
            RemoveChild(itemInHand);
            itemInHand = null;
            slot.InsertItem(item);
        }
        oldIndex = -1;
        
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
