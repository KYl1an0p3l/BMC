using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SettingsUi : Panel
{
    private const string remapPlaceholder = "Remapping...";

    private class RemapData
    {
        public string ActionName;
        public RichTextLabel Label;
    }

    private Dictionary<Button, RemapData> remapBindings = new();
    private bool useGamepadLabels = false;
    private Button lastRemapButton = null;

    private Dictionary<int, string> xboxButtonNames = new()
    {
        { 0, "A" }, { 1, "B" }, { 2, "X" }, { 3, "Y" },
        { 4, "Back" }, { 5, "RB" }, { 6, "Back" }, { 7, "Start" },
        { 8, "LStick" }, { 9, "LB" },
        { 10, "RB" }, { 11, "Haut" }, { 12, "Bas" }, { 13, "Gauche" }, { 14, "Droite" }
    };

    public void SetInputMode(bool isGamepad)
    {
        useGamepadLabels = isGamepad;
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        foreach (var entry in remapBindings)
        {
            entry.Value.Label.Text = GetActionLabel(entry.Value.ActionName);
        }
    }

    private string GetActionLabel(string action)
    {
        var events = InputMap.ActionGetEvents(action);
        var labels = new List<string>();
        foreach (var ev in events)
        {
            if (ev is InputEventJoypadButton joy)
                labels.Add(GetJoypadButtonName((int)joy.ButtonIndex));
            else if (ev is InputEventJoypadMotion motion)
                labels.Add(GetStickDirectionLabel(motion));
            else
                labels.Add(ev.AsText());
        }
        return string.Join(" / ", labels);
    }

    private string GetJoypadButtonName(int index)
    {
        return xboxButtonNames.TryGetValue(index, out var name) ? name : $"Bouton {index}";
    }

    private string GetStickDirectionLabel(InputEventJoypadMotion motion)
    {
        int axis = (int)motion.Axis;
        return (axis, motion.AxisValue) switch
        {
            (0, < 0) => "Stick gauche gauche",
            (0, > 0) => "Stick gauche droite",
            (1, < 0) => "Stick gauche haut",
            (1, > 0) => "Stick gauche bas",
            (2, < 0) => "Stick droit gauche",
            (2, > 0) => "Stick droit droite",
            (3, < 0) => "Stick droit haut",
            (3, > 0) => "Stick droit bas",
            _ => $"Axe {axis} {(motion.AxisValue > 0 ? "+" : "-")}"
        };
    }

    private RemapData currentRemap = null;
    private bool waitingForInput = false;

    public override void _Ready()
    {
        RegisterRemap("lookUpButton", "z");
        RegisterRemap("lookDownButton", "s");
        RegisterRemap("goLeftButton", "q");
        RegisterRemap("goRightButton", "d");
        RegisterRemap("jumpButton", "jump");
        RegisterRemap("atkButton", "atk");
        RegisterRemap("atkSecButton", "atk_sec");
        RegisterRemap("atkTerButton", "atk_ter");
        RegisterRemap("atkQuaButton", "atk_qua");
        RegisterRemap("invButton", "toggle_inventory_gui");
        RegisterRemap("absButton", "H");
        RegisterRemap("dashButton", "F");

        foreach (var entry in remapBindings)
        {
            entry.Key.FocusMode = Control.FocusModeEnum.All;
            entry.Key.Pressed += () => OnRemapButtonPressed(entry.Key);
        }

        remapBindings.Keys.First().GrabFocus();
    }

    private void RegisterRemap(string buttonPath, string actionName)
    {
        Button button = GetNode<Button>(buttonPath);
        RichTextLabel label = GetNode<RichTextLabel>($"{buttonPath}/inputLabel");
        remapBindings[button] = new RemapData { ActionName = actionName, Label = label };
    }

    private void OnRemapButtonPressed(Button button)
    {
        if (remapBindings.TryGetValue(button, out var remapData))
        {
            GD.Print(remapPlaceholder);
            remapData.Label.Text = remapPlaceholder;
            currentRemap = remapData;
            waitingForInput = true;
            lastRemapButton = button;

            foreach (var entry in remapBindings)
                entry.Key.FocusMode = Control.FocusModeEnum.None;

            button.ReleaseFocus();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (waitingForInput && currentRemap != null)
        {
            HandleRemapInput(@event);
            return;
        }

        if (@event is InputEventJoypadButton joypadButton && joypadButton.Pressed)
        {
            var focused = GetViewport().GuiGetFocusOwner();
            if (joypadButton.ButtonIndex == JoyButton.A && focused is Button button && remapBindings.ContainsKey(button))
            {
                button.EmitSignal("pressed");
            }
        }
    }

    private void HandleRemapInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (IsInputAlreadyUsed(keyEvent))
            {
                currentRemap.Label.Text = "Déjà utilisé !";
                CancelRemap();
                return;
            }
            ApplyRemap(currentRemap.ActionName, keyEvent, keyEvent.AsText());
        }
        else if (@event is InputEventJoypadButton joypadEvent && joypadEvent.Pressed)
        {
            if (IsInputAlreadyUsed(joypadEvent))
            {
                currentRemap.Label.Text = "Déjà utilisé !";
                CancelRemap();
                return;
            }
            ApplyRemap(currentRemap.ActionName, joypadEvent, GetJoypadButtonName((int)joypadEvent.ButtonIndex));
        }
        else if (@event is InputEventJoypadMotion motionEvent)
        {
            const float threshold = 0.5f;
            if (Mathf.Abs(motionEvent.AxisValue) > threshold)
            {
                if (IsInputAlreadyUsed(motionEvent))
                {
                    currentRemap.Label.Text = "Déjà utilisé !";
                    CancelRemap();
                    return;
                }
                ApplyRemap(currentRemap.ActionName, motionEvent, GetStickDirectionLabel(motionEvent));
            }
        }
    }

    private bool IsInputAlreadyUsed(InputEvent inputEvent)
    {
        foreach (var action in InputMap.GetActions())
        {
            if (action == currentRemap.ActionName)
                continue;

            foreach (var ev in InputMap.ActionGetEvents(action))
            {
                if (ev.Equals(inputEvent))
                    return true;
            }
        }
        return false;
    }

    private void ApplyRemap(string actionName, InputEvent inputEvent, string labelText)
    {
        InputMap.ActionEraseEvents(actionName);

        if (inputEvent is InputEventJoypadButton joypad)
        {
            var newEvent = new InputEventJoypadButton
            {
                ButtonIndex = joypad.ButtonIndex,
                Device = joypad.Device
            };
            InputMap.ActionAddEvent(actionName, newEvent);
        }
        else if (inputEvent is InputEventJoypadMotion motion)
        {
            var newEvent = new InputEventJoypadMotion
            {
                Axis = motion.Axis,
                AxisValue = motion.AxisValue,
                Device = motion.Device
            };
            InputMap.ActionAddEvent(actionName, newEvent);
        }
        else
        {
            InputMap.ActionAddEvent(actionName, inputEvent);
        }

        currentRemap.Label.Text = labelText;
        CancelRemap();
    }

    private async void CancelRemap()
    {
        var lastButton = remapBindings.FirstOrDefault(x => x.Value == currentRemap).Key;

        currentRemap = null;
        waitingForInput = false;

        // Réactiver le FocusMode pour tous les boutons
        foreach (var entry in remapBindings)
        {
            entry.Key.FocusMode = Control.FocusModeEnum.All;
        }

        // Attendre un petit délai pour éviter que la touche A relance un remap
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");

        // Redonner le focus au dernier bouton modifié
        if (lastButton != null)
        {
            lastButton.GrabFocus();
        }
    }

}
