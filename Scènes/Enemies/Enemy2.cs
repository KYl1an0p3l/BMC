using Godot;
using System;

public partial class Enemy2 : CharacterBody2D
{
    [Export] public float MoveSpeed = 100f;
    [Export] public int Health = 4;
    [Export] public NodePath PlayerPath;
    [Export] public PackedScene ammo;
    [Export] public NodePath LeftLimitPath;
    [Export] public NodePath RightLimitPath;
    private Node2D _leftLimit;
    private Node2D _rightLimit;
    private int direction = 1;
    private Vector2 velocity;
    private RayCast2D _rayCast;
    private Timer _timer;
    private Pp _player;
    private Pp _overlappingPlayer = null;

    public override void _Ready()
    {
        _rayCast = GetNode<RayCast2D>("RayCast2D");
        _timer = GetNode<Timer>("Timer");
        _player = GetNode<Pp>("../../Pp");
        _timer.Timeout += OnTimerTimeout;

        var damageArea = GetNode<Area2D>("DamageArea");
        damageArea.BodyEntered += OnBodyEntered;
        damageArea.BodyExited += OnBodyExited;

        _leftLimit = GetNode<Node2D>(LeftLimitPath);
        _rightLimit = GetNode<Node2D>(RightLimitPath);

        Velocity = Vector2.Zero;
    }

    public override void _PhysicsProcess(double delta)
    {
        Aim();
        CheckPlayerCollision();

        velocity.X = MoveSpeed * direction;
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
        Health -= amount;
        GD.Print($"Enemy2 touché ! Vie restante : {Health}");
        if (Health <= 0)
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
            Vector2 direction = (_player.GlobalPosition - GlobalPosition).Normalized();
            bulletScript.SetDirection(direction);
        }

        GetTree().CurrentScene.AddChild(bullet);
    }
}
