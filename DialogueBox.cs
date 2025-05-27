using Godot;
using System;

public partial class DialogueBox : CanvasLayer
{
    private Label dialogueLabel;

    public override void _Ready()
    {
        GD.Print("DialogueBox Ready: ", GetPath());

        dialogueLabel = GetNode<Label>("Panel/Label");
        Visible = false;
    }

    public void ShowDialogue(string text)
    {
        GD.Print("→ Dialogue affiché : ", text);
        dialogueLabel.Text = text;
        Visible = true;
    }

    public void HideDialogue()
    {
        Visible = false;
    }
}
