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
    public Texture2D HeartHalfTexture { get; set; }

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

        int fullHearts = currentHealth / 2;
        bool hasHalfHeart = currentHealth % 2 == 1;
        int totalHearts = (maxHearts + 1) / 2; // chaque cœur = 2 PV

        for (int i = 0; i < totalHearts; i++)
        {
            Node heartInstance = HeartScene.Instantiate();
            if (heartInstance is Panel panel && panel.GetNodeOrNull<Sprite2D>("Sprite2D") is Sprite2D sprite)
            {
                if (i < fullHearts)
                    sprite.Texture = HeartFullTexture;
                else if (i == fullHearts && hasHalfHeart)
                    sprite.Texture = HeartHalfTexture;
                else
                    sprite.Texture = HeartEmptyTexture;
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
