using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsUi : Panel
{
    private const string remapPlaceholder = "Remapping...";

    // Structure pour stocker les données liées à un bouton de remapping
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
        // Remap du bouton "Regarder en haut"
        Button lookUpButton = GetNode<Button>("lookUpButton");
        RichTextLabel lookUpLabel = GetNode<RichTextLabel>("lookUpButton/inputLabel");
        remapBindings[lookUpButton] = new RemapData { ActionName = "z", Label = lookUpLabel };

        // Remap du bouton "Regarder en bas"
        Button lookDownButton = GetNode<Button>("lookDownButton");
        RichTextLabel lookDownLabel = GetNode<RichTextLabel>("lookDownButton/inputLabel");
        remapBindings[lookDownButton] = new RemapData { ActionName = "s", Label = lookDownLabel };

        // Remap du bouton "Aller à gauche"
        Button goLeftButton = GetNode<Button>("goLeftButton");
        RichTextLabel goLeftLabel = GetNode<RichTextLabel>("goLeftButton/inputLabel");
        remapBindings[goLeftButton] = new RemapData { ActionName = "q", Label = goLeftLabel };

        // Remap du bouton "Aller à droite"
        Button goRightButton = GetNode<Button>("goRightButton");
        RichTextLabel goRightLabel = GetNode<RichTextLabel>("goRightButton/inputLabel");
        remapBindings[goRightButton] = new RemapData { ActionName = "d", Label = goRightLabel };

        // Remap du bouton "Sauter"
        Button jumpButton = GetNode<Button>("jumpButton");
        RichTextLabel jumpLabel = GetNode<RichTextLabel>("jumpButton/inputLabel");
        remapBindings[jumpButton] = new RemapData { ActionName = "jump", Label = jumpLabel };

        // Remap du bouton "Attaque"
        Button atkButton = GetNode<Button>("atkButton");
        RichTextLabel atkLabel = GetNode<RichTextLabel>("atkButton/inputLabel");
        remapBindings[atkButton] = new RemapData { ActionName = "atk", Label = atkLabel };

        // Remap du bouton "Attaque secondaire"
        Button atkSecButton = GetNode<Button>("atkSecButton");
        RichTextLabel atkSecLabel = GetNode<RichTextLabel>("atkSecButton/inputLabel");
        remapBindings[atkSecButton] = new RemapData { ActionName = "atk_sec", Label = atkSecLabel };

        // Remap du bouton "Attaque tertiaire"
        Button atkTerButton = GetNode<Button>("atkTerButton");
        RichTextLabel atkTerLabel = GetNode<RichTextLabel>("atkTerButton/inputLabel");
        remapBindings[atkTerButton] = new RemapData { ActionName = "atk_ter", Label = atkTerLabel };

        // Remap du bouton "Attaque quaternaire"
        Button atkQuaButton = GetNode<Button>("atkQuaButton");
        RichTextLabel atkQuaLabel = GetNode<RichTextLabel>("atkQuaButton/inputLabel");
        remapBindings[atkQuaButton] = new RemapData { ActionName = "atk_qua", Label = atkQuaLabel };

        // Remap du bouton "Inventaire"
        Button invButton = GetNode<Button>("invButton");
        RichTextLabel invLabel = GetNode<RichTextLabel>("invButton/inputLabel");
        remapBindings[invButton] = new RemapData { ActionName = "toggle_inventory_gui", Label = invLabel };

        // Remap du bouton "Absorption"
        Button absButton = GetNode<Button>("absButton");
        RichTextLabel absLabel = GetNode<RichTextLabel>("absButton/inputLabel");
        remapBindings[absButton] = new RemapData { ActionName = "abs", Label = absLabel };

        // Lier tous les boutons à un seul handler
        foreach (var entry in remapBindings)
            entry.Key.Pressed += () => OnRemapButtonPressed(entry.Key);
    }

    private void OnRemapButtonPressed(Button button)
    {
        if (remapBindings.TryGetValue(button, out var remapData))
        {
            GD.Print(remapPlaceholder);
            remapData.Label.Text = remapPlaceholder;
            currentRemap = remapData;
            waitingForInput = true;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!waitingForInput || currentRemap == null)
            return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            InputMap.ActionEraseEvents(currentRemap.ActionName);
            InputMap.ActionAddEvent(currentRemap.ActionName, keyEvent);
            currentRemap.Label.Text = OS.GetKeycodeString(keyEvent.Keycode);

            // Reset
            currentRemap = null;
            waitingForInput = false;
        }
    }
}
