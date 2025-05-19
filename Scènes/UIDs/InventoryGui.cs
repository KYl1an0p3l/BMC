using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public partial class InventoryGui : Control
{
    private Inventory inventory;
    private PackedScene itemStackLoad;
    private Godot.Collections.Array<Slot> slots = new();
    private ItemStackGui itemInHand;
    private int index, oldIndex;
    private bool locked = false;

    private int selectedIndex = 0;

    private Dictionary<int, Dictionary<string, int>> navigationMap = new()
    {
        { 0, new() { { "left", 18 }, { "right", 1 }, { "down", 5 } } },
        { 1, new() { { "left", 0 }, { "right", 2 }, { "down", 6 } } },
        { 2, new() { { "left", 1 }, { "right", 3 }, { "down", 7 } } },
        { 3, new() { { "left", 2 }, { "right", 4 }, { "down", 8 }} },
        { 4, new() { { "left", 3 }, { "right", 18 }, { "down", 9 }} },
        { 5, new() { { "left", 17 }, { "right", 6 }, { "down", 10 }, { "up", 0 } } },
        { 6, new() { { "left", 5 }, { "right", 7 }, { "down", 11 }, { "up", 1 } } },
        { 7, new() { { "left", 6 }, { "right", 8 }, { "down", 12 }, { "up", 2 } } },
        { 8, new() { { "left", 7 }, { "right", 9 }, { "down", 13 }, { "up", 3 } } },
        { 9, new() { { "left", 8 }, { "right", 15 }, { "down", 14 }, { "up", 4 } } },
        { 10, new() { { "left", 17 }, { "right", 11 }, { "down", 0 }, { "up", 5 } } },
        { 11, new() { { "left", 10 }, { "right", 12 }, { "down", 1 }, { "up", 6 } } },
        { 12, new() { { "left", 11 }, { "right", 13 }, { "down", 2 }, { "up", 7 } } },
        { 13, new() { { "left", 12 }, { "right", 14 }, { "down", 3 }, { "up", 8 } } },
        { 14, new() { { "left", 13 }, { "right", 16 }, { "down", 4 }, { "up", 9 } } },
        { 15, new() { { "left", 9 }, { "right", 17 }, { "down", 16 }, { "up", 18 } } },
        { 16, new() { { "left", 15 }, { "right", 17 }, { "down", 18 }, { "up", 18 } } },
        { 17, new() { { "left", 15 }, { "right", 5 }, { "down", 16 }, { "up", 18 } } },
        { 18, new() { { "left", 15 }, { "right", 17 }, { "down", 16 }, { "up", 16 } } }
    };

    public override void _Ready()
    {
        Visible = false;
        oldIndex = -1;
        index = 0;
        inventory = GameState.Instance.PlayerInventory;
        itemStackLoad = GD.Load<PackedScene>("res://Scènes/UIDs/itemStackGui.tscn");

        var slotNodes = GetNode<Panel>("NinePatchRect/GridContainer").GetChildren();
        foreach (Node node in slotNodes)
        {
            if (node is Slot slot)
            {
                slot.index = index;
                slots.Add(slot);
                int capturedIndex = index;
                slot.Pressed += () => onSlotClicked(slots[capturedIndex]);
                index++;
            }
        }

        index = 0;
        inventory.OnUpdated += UpdateInventory;
        UpdateInventory();

        if (slots.Count > 0)
            slots[selectedIndex].SetSelected(true);
    }

    public void toggle_inventory_gui()
    {
        if (Input.IsActionJustPressed("toggle_inventory_gui"))
        {
            Visible = !Visible;

            if (Visible && slots.Count > 0)
            {
                foreach (var slot in slots)
                    slot.SetSelected(false);

                selectedIndex = 0;
                slots[selectedIndex].SetSelected(true);
            }
        }
    }

    public void UpdateInventory()
    {
        for (int i = 0; i < Math.Min(inventory.Slots.Count, slots.Count); i++)
        {
            if (inventory == null || inventory.Slots[i] == null || inventory.Slots[i].Item == null)
                continue;

            InventorySlot inventorySlot = inventory.Slots[i];
            ItemStackGui isg = slots[i].itemStack;

            if (isg == null)
            {
                isg = itemStackLoad.Instantiate<ItemStackGui>();
                slots[i].InsertItem(isg);
            }

            if (inventorySlot == null || isg == null)
                continue;

            isg.slot = inventorySlot;
            isg.UpdateItems(inventorySlot);
        }
    }

    public void onSlotClicked(Slot slot)
    {
        if (locked) return;

        if (slot.isEmpty() && itemInHand != null)
        {
            insertItemInSlot(slot);
            return;
        }
        else if (itemInHand == null)
        {
            takeItemFromSlot(slot);
            return;
        }

        swapItems(slot);
    }

    public void UpdateItemInHand()
    {
        if (itemInHand != null)
            itemInHand.GlobalPosition = GetGlobalMousePosition() - itemInHand.Size / 2;
    }

    public async Task putItemBack()
    {
        locked = true;

        if (oldIndex < 0)
        {
            var emptySlots = slots.Where(s => s.isEmpty()).ToList();
            if (!emptySlots.Any())
                return;

            oldIndex = emptySlots[0].index;
        }

        var targetSlot = slots[oldIndex];
        var tween = CreateTween();
        var targetPosition = targetSlot.GlobalPosition + targetSlot.Size / 2;
        tween.TweenProperty(itemInHand, "global_position", targetPosition, 0.2);
        await ToSignal(tween, Tween.SignalName.Finished);

        insertItemInSlot(targetSlot);
        locked = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;

        if (Input.IsActionJustPressed("rightClick") && !locked && itemInHand != null)
        {
            _ = putItemBack();
        }

        if (@event is InputEventJoypadButton joypadButton && joypadButton.Pressed)
        {
            string direction = null;

            switch (joypadButton.ButtonIndex)
            {
                case JoyButton.DpadRight: direction = "right"; break;
                case JoyButton.DpadLeft: direction = "left"; break;
                case JoyButton.DpadDown: direction = "down"; break;
                case JoyButton.DpadUp: direction = "up"; break;
                case JoyButton.A:
                    onSlotClicked(slots[selectedIndex]);
                    return;
            }

            if (direction != null &&
                navigationMap.ContainsKey(selectedIndex) &&
                navigationMap[selectedIndex].ContainsKey(direction))
            {
                slots[selectedIndex].SetSelected(false);
                selectedIndex = navigationMap[selectedIndex][direction];
                slots[selectedIndex].SetSelected(true);
            }
        }

        UpdateItemInHand();
    }

    public void takeItemFromSlot(Slot slot)
    {
        itemInHand = slot.takeItem();
        if (itemInHand != null)
        {
            AddChild(itemInHand);
            UpdateItemInHand();
        }
        oldIndex = slot.index;
    }

    public void insertItemInSlot(Slot slot)
    {
        var item = itemInHand;
        if (itemInHand != null)
        {
            RemoveChild(itemInHand);
            itemInHand = null;
            slot.InsertItem(item);
        }
        oldIndex = -1;
    }

    public void swapItems(Slot slot)
    {
        var tempItem = slot.takeItem();
        insertItemInSlot(slot);
        itemInHand = tempItem;
        if (itemInHand != null)
        {
            AddChild(itemInHand);
        }
        UpdateItemInHand();
    }
}
