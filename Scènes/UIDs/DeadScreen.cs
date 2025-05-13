using Godot;
using System;

public partial class DeadScreen : Control
{
    private Panel deathScreen;
    private Button restartButton;
    private AnimationPlayer animationPlayer;
    private PackedScene restartScene;
    public override void _Ready(){
        deathScreen = GetNode<Panel>("screenPanel");
        animationPlayer = GetNode<AnimationPlayer>("screenAnimation");
        restartButton = GetNode<Button>("screenPanel/screenVBox/deadButton");
        //restartScene = GD.Load<PackedScene>("res://Scènes/Maps/dev_test_map.tscn"); version manuelle (au cas où)
        restartButton.Pressed += load_restart_scene;
        deathScreen.Visible = false;
    }

    public void death_screen(){
        deathScreen.Visible = true;
        animationPlayer.Play("show");
        if(Input.IsActionPressed("ui_right")){
            deathScreen.Visible = false;
        }
    }
    public bool IsDeathScreenVisible()
    {
        return deathScreen.Visible;
    }
    private void load_restart_scene(){
        GetTree().ReloadCurrentScene();
        //GetTree().ChangeSceneToPacked(restartScene);
    }
}
