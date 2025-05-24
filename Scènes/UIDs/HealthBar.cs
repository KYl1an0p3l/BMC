using Godot;
using System;

public partial class HealthBar : HBoxContainer
{
    [Export]
    public PackedScene HeartScene { get; set; }

    private int maxHearts = 0;

    [Export]
    public Texture2D HeartFullTexture { get; set; }

    [Export]
    public Texture2D HeartEmptyTexture { get; set; }

    public void UpdateHearts(int currentHealth)
    {
        ClearChildren();

        if (HeartScene == null)
        {
            GD.PrintErr("HeartScene n'est pas assignée !");
            return;
        }

        int totalHearts = maxHearts; // 1 cœur = 1 PV
        int fullHearts = currentHealth;

        for (int i = 0; i < totalHearts; i++)
        {
            Node heartInstance = HeartScene.Instantiate();
            if (heartInstance is Panel panel && panel.GetNodeOrNull<Sprite2D>("Sprite2D") is Sprite2D sprite)
            {
                sprite.Texture = (i < fullHearts) ? HeartFullTexture : HeartEmptyTexture;
            }
            AddChild(heartInstance);
        }
    }

    public void SetMaxHearts(int max)
    {
        maxHearts = max;
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