using Godot;
using System;
using System.Runtime.InteropServices;

[Tool]
[GlobalClass]

public partial class TileData : Resource
{
    [Export] public PackedScene Scene;
    [Export] public bool OpenTop;
    [Export] public bool OpenRight;
    [Export] public bool OpenBottom;
    [Export] public bool OpenLeft;
}
