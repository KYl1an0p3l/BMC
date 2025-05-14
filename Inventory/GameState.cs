using Godot;
using System;

public partial class GameState : Node
{
    public static GameState Instance;

    public Inventory PlayerInventory;

    public override void _EnterTree()
    {
        Instance = this;
    }
    public override void _Ready()
    {
        PlayerInventory = GD.Load<Inventory>("res://Inventory/playerInventory.tres");
    }

}
