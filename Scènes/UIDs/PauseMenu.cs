using Godot;
using System;
using System.Transactions;

public partial class PauseMenu : Control
{
    private Button _resumeButton;
    private Button _settingsButton;
    private Button _quitButton;
    private Button _settingsReturnButton;
    private Panel settingsUi, screenPanel;

    public override void _Ready()
    {
        _resumeButton = GetNode<Button>("screenPanel/screenVBox/ResumeButton");
        _settingsButton = GetNode<Button>("screenPanel/screenVBox/SettingsButton");
        _quitButton = GetNode<Button>("screenPanel/screenVBox/QuitButton");
        _settingsReturnButton = GetNode<Button>("settingsUi/returnButton");
        settingsUi = GetNode<Panel>("settingsUi");
        screenPanel = GetNode<Panel>("screenPanel");

        screenPanel.Visible = true;
        settingsUi.Visible = false;

        _resumeButton.Pressed += OnResumePressed;
        _settingsButton.Pressed += OnSettingsPressed;
        _quitButton.Pressed += OnQuitPressed;
        _settingsReturnButton.Pressed += OnSettingsReturnPressed;

        Hide(); // Cache le menu au début
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            var canvasLayer = GetParent(); // PauseMenu est enfant de CanvasLayer
            var deadScreenNode = canvasLayer.GetNodeOrNull<DeadScreen>("DeadScreen");

            if (deadScreenNode != null && !deadScreenNode.IsDeathScreenVisible())
            {
                if (Visible && !settingsUi.Visible)
                {
                    GetTree().Paused = false;
                    Hide();
                }
                else if (Visible && settingsUi.Visible)
                {
                    OnSettingsReturnPressed();
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

    private void OnSettingsPressed()
    {
        screenPanel.Visible = false;
        settingsUi.Visible = true;
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnSettingsReturnPressed()
    {
        screenPanel.Visible = true;
        settingsUi.Visible = false;
    }
}
