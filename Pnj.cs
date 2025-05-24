using Godot;
using System;

public partial class Pnj : Node2D
{
    [Export] public string dialogueText = "Bonjour, aventurier !";

    private Label interactionLabel;
    private bool playerInZone = false;
    private DialogueBox dialogueBox;

    public override void _Ready()
    {
        interactionLabel = GetNode<Label>("Label");
        interactionLabel.Visible = false;

        GetNode<Area2D>("Area2D").BodyEntered += OnBodyEntered;
        GetNode<Area2D>("Area2D").BodyExited += OnBodyExited;

        dialogueBox = GetNode<DialogueBox>("../PP/CanvasLayer/DialogueBox"); // adapte le chemin
    }

    public override void _Process(double delta)
    {
        if (playerInZone && (Input.IsActionJustPressed("ui_accept") || Input.IsActionJustPressed("interact")))
        {
            if (dialogueBox.Visible)
            {
                dialogueBox.HideDialogue();
            }
            else
            {
                dialogueBox.ShowDialogue(dialogueText);
            }
        }
    }

    private void OnBodyEntered(Node body)
    {
        if (body is CharacterBody2D)
        {
            playerInZone = true;
            interactionLabel.Visible = true;
        }
    }

    private void OnBodyExited(Node body)
    {
        if (body is CharacterBody2D)
        {
            playerInZone = false;
            interactionLabel.Visible = false;

            if (dialogueBox.Visible)
                dialogueBox.HideDialogue();
        }
    }
}
