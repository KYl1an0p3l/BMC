using Godot;
using System;

public partial class PauseMenu : Control
{
    private Button _resumeButton;
    private Button _quitButton;

    public override void _Ready()
    {
        _resumeButton = GetNode<Button>("screenPanel/screenVBox/ResumeButton");
        _quitButton = GetNode<Button>("screenPanel/screenVBox/QuitButton");

        _resumeButton.Pressed += OnResumePressed;
        _quitButton.Pressed += OnQuitPressed;

        Hide(); // Cache le menu au début
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("echap"))
        {
            var canvasLayer = GetParent(); // PauseMenu est enfant de CanvasLayer
            var deadScreenNode = canvasLayer.GetNodeOrNull<DeadScreen>("DeadScreen");

            if (deadScreenNode != null && !deadScreenNode.IsDeathScreenVisible())
            {
                if (Visible)
                {
                    GetTree().Paused = false;
                    Hide();
                }
                else
                {
                    GetTree().Paused = true;
                    Show();
                }
            }
        }
    }


    private void OnResumePressed()
    {
        GetTree().Paused = false;
        Hide();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
