using Godot;
using System;

public partial class HealthBar : HBoxContainer
{
    [Export]
    public PackedScene HeartScene { get; set; }

    private int currentHeartCount = 0;
    public void UpdateHearts(int currentHealth){
        ClearChildren();
        if (HeartScene == null){
            GD.PrintErr("HeartScene n'est pas assignée !");
            return;
        }
        for (int i = 0; i < currentHealth; i++){
            Node heartInstance = HeartScene.Instantiate();
            AddChild(heartInstance);
        }
        currentHeartCount = currentHealth;
    }
    private void ClearChildren(){
        foreach (Node child in GetChildren()){
            RemoveChild(child);
            child.QueueFree();
        }
    }
}