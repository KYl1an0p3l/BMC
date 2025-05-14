using Godot;
using System;

public partial class HealthBar : HBoxContainer
{
    [Export]
    public PackedScene HeartScene { get; set; }

    [Export]
    public int MaxHealth { get; set; } = 5;

    [Export]
    public Texture2D HeartFullTexture { get; set; }

    [Export]
    public Texture2D HeartEmptyTexture { get; set; }

    public void UpdateHearts(int currentHealth)
    {
        ClearChildren();
        if (HeartScene == null || HeartFullTexture == null || HeartEmptyTexture == null)
        {
            GD.PrintErr("HeartScene ou texture(s) non assignée(s) !");
            return;
        }

        for (int i = 0; i < MaxHealth; i++)
        {
            Node heartInstance = HeartScene.Instantiate();
            if (heartInstance is Panel panel && panel.GetNodeOrNull<Sprite2D>("Sprite2D") is Sprite2D sprite)
            {
                sprite.Texture = i < currentHealth ? HeartFullTexture : HeartEmptyTexture;
            }
            AddChild(heartInstance);
        }
    }

    private void ClearChildren()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
    }
}
