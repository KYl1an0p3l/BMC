using Godot;
using System;

[Tool] // Pour que ça s'exécute dans l'éditeur
[GlobalClass] // Pour que ce soit une ressource visible comme un type exportable
public partial class InventoryItems : Resource
{
    [Export] public String Name;
    [Export] public Texture2D Texture;
    [Export] public String ActionName;

    public InventoryItems DeepCopy()
    {
        return (InventoryItems)this.Duplicate(true);
    }
}
