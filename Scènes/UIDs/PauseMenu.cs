using Godot;
using System;

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
        if (Input.IsActionJustPressed("echap"))
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
                    _resumeButton.GrabFocus(); // Focus automatique sur "Résumé"
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible)
            return;

        if (@event is InputEventJoypadButton joypadButton && joypadButton.Pressed)
        {
            // Bouton A / Croix → activer le bouton sélectionné
            if (joypadButton.ButtonIndex == JoyButton.A)
            {
                var focused = GetViewport().GuiGetFocusOwner();
                if (focused is Button button)
                    button.EmitSignal("pressed");
            }

            // Bouton B / Rond → retour ou quitter le menu
            if (joypadButton.ButtonIndex == JoyButton.B)
            {
                if (settingsUi.Visible)
                    OnSettingsReturnPressed();
                else
                    OnResumePressed();
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

        // Focus automatique sur le premier bouton des settings
        var firstSettingButton = settingsUi.GetNodeOrNull<Button>("lookUpButton");
        firstSettingButton?.GrabFocus();
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }

    private void OnSettingsReturnPressed()
    {
        screenPanel.Visible = true;
        settingsUi.Visible = false;
        _settingsButton.GrabFocus(); // Revenir sur le bouton "Paramètres"
    }
}
