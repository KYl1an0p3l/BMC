using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class Inventory : Resource
{
    [Export]
    public Godot.Collections.Array<InventoryItems> Items { get; set; } = new();

    public void Insert(InventoryItems item){
        
    }
}
