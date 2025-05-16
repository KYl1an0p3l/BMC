using Godot;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class Slot : Button
{
    private Sprite2D backgroundSprite;
    private CenterContainer container;
    private Inventory inventory;
    public ItemStackGui itemStack;
    public int index;
    public override void _Ready()
    {
        backgroundSprite = GetNode<Sprite2D>("background");
        container = GetNode<CenterContainer>("CenterContainer");
        inventory = GameState.Instance.PlayerInventory;
    }

    public void InsertItem(ItemStackGui isg){
        if (itemStack != null)
            itemStack.QueueFree(); //Nettoyage s'il y avait un ancien item dans la mémoire
        itemStack = isg;
        container.AddChild(isg);
        if(itemStack.slot == null || inventory.Slots[index] == itemStack.slot){
            return;
        }
        inventory.insertSlot(index, itemStack.slot);
    }

    public ItemStackGui takeItem(){
        var item = itemStack;
        if(itemStack != null){
            container.RemoveChild(itemStack);
            itemStack = null;
            return item;
        }
        else{
            return null;
        }
    }

    public bool isEmpty(){
        //au cas où pour debbug : return (itemStack == null) ? true : false;
        return itemStack == null;
    }
}
