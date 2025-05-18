using Godot;
using System;

public partial class DeadScreen : Control
{
    private Panel deathScreen;
    private Button restartButton;
    private AnimationPlayer animationPlayer;

    public override void _Ready()
    {
        deathScreen = GetNode<Panel>("screenPanel");
        animationPlayer = GetNode<AnimationPlayer>("screenAnimation");
        restartButton = GetNode<Button>("screenPanel/screenVBox/deadButton");

        restartButton.Pressed += LoadRestartScene;
        deathScreen.Visible = false;
    }

    public void death_screen()
    {
        deathScreen.Visible = true;
        animationPlayer.Play("show");

        // Focus automatique sur le bouton "Recommencer"
        restartButton.GrabFocus();
    }

    public bool IsDeathScreenVisible()
    {
        return deathScreen.Visible;
    }

    public override void _Input(InputEvent @event)
    {
        if (!deathScreen.Visible)
            return;

        if (@event is InputEventJoypadButton joypadButton && joypadButton.Pressed)
        {
            // A / Croix → activer le bouton sélectionné
            if (joypadButton.ButtonIndex == JoyButton.A)
            {
                var focused = GetViewport().GuiGetFocusOwner();
                if (focused is Button button)
                    button.EmitSignal("pressed");
            }

            // B / Rond → cacher l'écran de mort (optionnel)
            if (joypadButton.ButtonIndex == JoyButton.B)
            {
                deathScreen.Visible = false;
            }
        }
    }

    private void LoadRestartScene()
    {
        GetTree().ReloadCurrentScene();
    }
}
