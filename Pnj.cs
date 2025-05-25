using Godot;
using System;

public partial class Pnj : Node2D
{
	[Export] public string dialogueText = "Salut toi, t'as un style vraiment particulier, ça fait longtemps qu'on a pas vu d'Exilé par ici. Bon, si t'arrives pas à aller plus loin, tu finiras ton sort ici. Y'a un type qui se faisait appeler 'Journaliste', il a jamais réussis à passer cette embûche, résultat : il est mort dans le caniveau un peu plus loin. On dit qu'il s'est transformé en je sais pas trop quoi. Penses à t'équiper avant d'y aller !";

	private Label interactionLabel;
	private bool playerInZone = false;
	private DialogueBox dialogueBox;

	public override void _Ready()
	{
		interactionLabel = GetNode<Label>("Label");
		interactionLabel.Visible = false;

		GetNode<Area2D>("Area2D").BodyEntered += OnBodyEntered;
		GetNode<Area2D>("Area2D").BodyExited += OnBodyExited;

		dialogueBox = GetNode<DialogueBox>("../PP/CanvasLayer/DialogueBox");
	}

	public override void _Process(double delta)
	{
		if (playerInZone && (Input.IsActionJustPressed("ui_accept") || Input.IsActionJustPressed("interact")))
		{
			if (dialogueBox != null)
			{
				if (dialogueBox.Visible)
					dialogueBox.HideDialogue();
				else
					ShowDialogueOverlay();
			}
			
		}
	}

	private void OnBodyEntered(Node body)
	{
		GD.Print("Body entered: ", body.Name, " (", body.GetPath(), ")");
		dialogueBox = body.GetNodeOrNull<DialogueBox>("CanvasLayer/DialogueBox");
		if (dialogueBox == null)
		{
			GD.PrintErr("→ DialogueBox introuvable dans le joueur !");
		}
		else
		{
			GD.Print("→ DialogueBox trouvée : ", dialogueBox.Name);
		}

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
	private void ShowDialogueOverlay()
	{
		if (dialogueBox == null)
			return;

		// 1) On retire du joueur
		var oldParent = dialogueBox.GetParent();
		oldParent.RemoveChild(dialogueBox);

		// 2) On l'ajoute à la racine de la scène
		GetTree().Root.AddChild(dialogueBox);

		// 3) On la place en bas-centre de l'écran
		var panel = dialogueBox.GetNode<Control>("Panel");
		var screenSize = GetViewport().GetVisibleRect().Size;
		panel.Position = new Vector2(
			(screenSize.X - panel.Size.X) / 2,
			screenSize.Y - panel.Size.Y - 20
		);

		dialogueBox.ShowDialogue(dialogueText);
	}
}
