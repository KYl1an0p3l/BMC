using Godot;
using System;

public partial class Slot : Button
{
    private Sprite2D backgroundSprite;
    private Sprite2D selectedSprite;
    private CenterContainer container;
    private Inventory inventory;
    public ItemStackGui itemStack;
    public int index;

    public override void _Ready()
    {
        backgroundSprite = GetNode<Sprite2D>("background");
        selectedSprite = GetNode<Sprite2D>("selected");
        container = GetNode<CenterContainer>("CenterContainer");
        inventory = GameState.Instance.PlayerInventory;

        SetSelected(false); // Par défaut, non sélectionné
    }

    public void SetSelected(bool selected)
    {
        if (selectedSprite != null)
            selectedSprite.Visible = selected;
    }

    public void InsertItem(ItemStackGui isg)
    {
        if (itemStack != null)
            itemStack.QueueFree();
        itemStack = isg;
        container.AddChild(isg);
        if (itemStack.slot == null || inventory.Slots[index] == itemStack.slot)
            return;
        inventory.insertSlot(index, itemStack.slot);
    }

    public ItemStackGui takeItem()
    {
        var item = itemStack;
        if (itemStack != null)
        {
            container.RemoveChild(itemStack);
            itemStack = null;
            return item;
        }
        return null;
    }

    public bool isEmpty()
    {
        return itemStack == null;
    }
}

