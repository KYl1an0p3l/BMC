using Godot;
using System;

public partial class DialogueBox : CanvasLayer
{
    private Label dialogueLabel;

    public override void _Ready()
    {
        dialogueLabel = GetNode<Label>("Panel/Label");
        Visible = false;
    }

    public void ShowDialogue(string text)
    {
        dialogueLabel.Text = text;
        Visible = true;
    }

    public void HideDialogue()
    {
        Visible = false;
    }
}
