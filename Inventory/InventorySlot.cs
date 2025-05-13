using Godot;
using System;

public partial class InventorySlot : Resource
{
    [Export] public InventoryItems Item;
    [Export] public int amount;
}
