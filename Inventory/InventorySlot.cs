using Godot;
using System;

[Tool]
[GlobalClass]
public partial class InventorySlot : Resource
{
    [Export] public InventoryItems Item;
    [Export] public int amount;
}
