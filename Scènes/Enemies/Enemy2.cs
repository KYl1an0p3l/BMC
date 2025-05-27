using Godot;
using System;

public partial class Enemy2 : CharacterBody2D
{
	[Export] private Ennemies ennemy;
	[Export] public NodePath PlayerPath;
	[Export] public PackedScene ammo;
	[Export] public NodePath LeftLimitPath;
	[Export] public NodePath RightLimitPath;
	private Node2D _leftLimit;
	private Node2D _rightLimit;
	private int direction = 1;
	private Vector2 velocity;
	private RayCast2D _rayCast;
	private AnimatedSprite2D animatedSprite2D;
	private Timer _timer;
	private Pp _player;
	private Pp _overlappingPlayer = null;

	public override void _Ready()
	{
		var players = GetTree().GetNodesInGroup("player");
		if (players.Count > 0 && players[0] is Pp player)
		{
			_player = player;
		}
		_rayCast = GetNode<RayCast2D>("RayCast2D");
		_timer = GetNode<Timer>("Timer");

		_timer.Timeout += OnTimerTimeout;

		var damageArea = GetNode<Area2D>("DamageArea");
		damageArea.BodyEntered += OnBodyEntered;
		damageArea.BodyExited += OnBodyExited;

		_leftLimit = GetNode<Node2D>(LeftLimitPath);
		_rightLimit = GetNode<Node2D>(RightLimitPath);
		animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		Velocity = Vector2.Zero;
	}

	public override void _PhysicsProcess(double delta)
	{
		Aim();
		CheckPlayerCollision();

		velocity.X = ennemy.Speed * direction;
		Velocity = velocity;
		MoveAndSlide();

		// Obtenir les limites gauche/droite, quelle que soit leur position
		float leftX = Mathf.Min(_leftLimit.GlobalPosition.X, _rightLimit.GlobalPosition.X);
		float rightX = Mathf.Max(_leftLimit.GlobalPosition.X, _rightLimit.GlobalPosition.X);

		// Inverser direction si on atteint une des bornes
		if (direction < 0 && GlobalPosition.X <= _leftLimit.GlobalPosition.X)
		{
			direction = 1;
			GlobalPosition = new Vector2(leftX, GlobalPosition.Y);
		}
		else if (direction > 0 && GlobalPosition.X >= _rightLimit.GlobalPosition.X)
		{
			direction = -1;
			GlobalPosition = new Vector2(rightX, GlobalPosition.Y);
		}

		if (direction == -1)
			animatedSprite2D.Play("left");
		else if (direction == 1)
			animatedSprite2D.Play("right");

	}

	private void Aim()
	{
		if (!IsInstanceValid(_player)) return;

		Vector2 aimDirection = (_player.GlobalPosition - GlobalPosition).Normalized();
		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
		_rayCast.TargetPosition = aimDirection * distance;
	}

	private void CheckPlayerCollision()
	{
		if (_rayCast.GetCollider() == _player && _timer.IsStopped())
		{
			_timer.Start();
		}
		else if (_rayCast.GetCollider() != _player && !_timer.IsStopped())
		{
			_timer.Stop();
		}
	}

	private void OnBodyEntered(Node body)
	{
		if (body is Pp player)
		{
			_overlappingPlayer = player;
			if (!player.IsInvincible())
			{
				player.TakeDamage(1);
			}
		}
	}

	private void OnBodyExited(Node body)
	{
		if (body == _overlappingPlayer)
		{
			_overlappingPlayer = null;
		}
	}

	public void TakeDamage(int amount)
	{
		ennemy.Health -= amount;
		GD.Print($"Enemy2 touché ! Vie restante : {ennemy.Health}");
		if (ennemy.Health <= 0)
		{
			QueueFree();
		}
	}

	private void OnTimerTimeout()
	{
		Shoot();
	}

	private void Shoot()
	{
		if (ammo == null) return;
		GD.Print("Tir !");
		Node2D bullet = (Node2D)ammo.Instantiate();
		bullet.GlobalPosition = GlobalPosition;

		if (bullet is Bullets bulletScript)
		{
			if (_player != null)
			{
				Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
				bulletScript.SetDirection(direction);
			}
		}
		GetTree().CurrentScene.AddChild(bullet);
	}
}
