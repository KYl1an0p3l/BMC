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
        RegisterRemap("absButton", "abs");
        RegisterRemap("dashButton", "F");

        foreach (var entry in remapBindings)
        {
            entry.Key.FocusMode = Control.FocusModeEnum.All;
            entry.Key.Pressed += () => OnRemapButtonPressed(entry.Key);
        }

        // Focus initial automatique pour la manette
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
            button.FocusMode = Control.FocusModeEnum.None;
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

            ApplyRemap(currentRemap.ActionName, keyEvent, OS.GetKeycodeString(keyEvent.Keycode));
        }
        else if (@event is InputEventJoypadButton joypadEvent && joypadEvent.Pressed)
        {
            if (IsInputAlreadyUsed(joypadEvent))
            {
                currentRemap.Label.Text = "Déjà utilisé !";
                CancelRemap();
                return;
            }

            ApplyRemap(currentRemap.ActionName, joypadEvent, $"Joypad Button {joypadEvent.ButtonIndex}");
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

                var motion = new InputEventJoypadMotion
                {
                    Axis = motionEvent.Axis,
                    AxisValue = motionEvent.AxisValue,
                    Device = motionEvent.Device
                };

                string direction = motionEvent.AxisValue > 0 ? "+" : "-";
                ApplyRemap(currentRemap.ActionName, motion, $"Joypad Axis {motionEvent.Axis} {direction}");
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
        InputMap.ActionAddEvent(actionName, inputEvent);
        currentRemap.Label.Text = labelText;
        CancelRemap();
    }

    private void CancelRemap()
    {
        foreach (var entry in remapBindings)
        {
            entry.Key.FocusMode = Control.FocusModeEnum.All;
        }
        currentRemap = null;
        waitingForInput = false;
    }
}
