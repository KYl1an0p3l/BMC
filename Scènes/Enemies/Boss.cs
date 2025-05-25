using Godot;
using System;

public partial class Boss : CharacterBody2D
{
    [Export] public int Health = 6;
    [Export] public float Speed = 200f;
    [Export] public float Gravity = 800f;
    [Export] public float MaxFallSpeed = 400f;
    [Export] public PackedScene ammo;

    private int maxHealth;
    private Vector2 _velocity = Vector2.Zero;
    private Vector2 direction = Vector2.Left;
    private AnimatedSprite2D Sprite;
    private RayCast2D rayLeft;
    private RayCast2D rayRight;
    private RayCast2D attackLeft;
    private RayCast2D attackRight;
    private ProgressBar bossBar;

    private Pp overlappingPlayer = null;
    private bool isMoving = true;
    private Timer shootCooldownTimer;
    private bool canShoot = true;

    private Timer jumpTimer;
    private bool isJumping = false;
    private float jumpSpeed = -500f;
    private float jumpSlowdownThreshold = -50f;

    public override void _Ready()
    {
        maxHealth = Health;

        Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        rayLeft = GetNode<RayCast2D>("RayLeft");
        rayRight = GetNode<RayCast2D>("RayRight");
        attackLeft = GetNode<RayCast2D>("AttackLeft");
        attackRight = GetNode<RayCast2D>("AttackRight");
        bossBar = GetNode<ProgressBar>("BossBar");

        bossBar.MaxValue = maxHealth;
        bossBar.Value = Health;

        var damageArea = GetNode<Area2D>("DamageArea");
        damageArea.BodyEntered += OnBodyEntered;
        damageArea.BodyExited += OnBodyExited;

        shootCooldownTimer = new Timer();
        shootCooldownTimer.OneShot = true;
        shootCooldownTimer.WaitTime = 5.0f;
        shootCooldownTimer.Timeout += () => canShoot = true;
        AddChild(shootCooldownTimer);

        jumpTimer = new Timer();
        jumpTimer.OneShot = true;
        jumpTimer.Timeout += OnJumpTimerTimeout;
        AddChild(jumpTimer);
        StartJumpTimer();

        CollisionLayer = 1 << 2;
        CollisionMask = 1 << 0;

        attackLeft.Enabled = true;
        attackRight.Enabled = true;
    }

    public override void _Process(double delta)
    {
        if (overlappingPlayer != null && !overlappingPlayer.IsInvincible())
        {
            overlappingPlayer.TakeDamage(1);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (isJumping && _velocity.Y < jumpSlowdownThreshold)
        {
            _velocity.Y += (Gravity * 0.3f) * (float)delta;
        }
        else
        {
            _velocity.Y += Gravity * (float)delta;
        }

        if (_velocity.Y > MaxFallSpeed)
            _velocity.Y = MaxFallSpeed;

        if (IsOnFloor())
            isJumping = false;

        rayLeft.Enabled = !isJumping;
        rayRight.Enabled = !isJumping;
        if (IsOnFloor() && !isJumping)
        {
            
            _velocity.X = isMoving ? direction.X * Speed : 0f;
        }
        Velocity = _velocity;
        MoveAndSlide();
        Position = new Vector2(Mathf.Round(Position.X), Mathf.Round(Position.Y));
        _velocity = Velocity;

        var player = GetTree().GetFirstNodeInGroup("player") as Pp;
        if (player != null)
        {
            if (player.GlobalPosition.X < GlobalPosition.X)
                Sprite.Play("left");
            else
                Sprite.Play("right");
        }

        if (isMoving)
        {
            if (direction == Vector2.Left && !rayLeft.IsColliding())
                direction = Vector2.Right;
            else if (direction == Vector2.Right && !rayRight.IsColliding())
                direction = Vector2.Left;
        }

        if (canShoot)
        {
            if (attackLeft.IsColliding() && attackLeft.GetCollider() is Pp)
            {
                GD.Print("Détection à gauche !");
                Shoot(Vector2.Left);
            }
            else if (attackRight.IsColliding() && attackRight.GetCollider() is Pp)
            {
                GD.Print("Détection à droite !");
                Shoot(Vector2.Right);
            }
        }
    }

    private void Shoot(Vector2 direction)
    {
        if (ammo == null)
        {
            GD.PrintErr("Aucune scène de munition assignée !");
            return;
        }

        GD.Print("Boss tire vers : " + direction);
        Node2D bullet = (Node2D)ammo.Instantiate();
        bullet.GlobalPosition = GlobalPosition + direction * 10f;

        if (bullet is Bullets bulletScript)
        {
            bulletScript.SetDirection(direction);
        }

        GetTree().CurrentScene.AddChild(bullet);
        canShoot = false;
        shootCooldownTimer.Start();
    }

    private void Jump()
    {
        if (IsOnFloor())
        {
            _velocity.Y = jumpSpeed;
            isJumping = true;
        }
    }

    private void StartJumpTimer()
    {
        var random = new Random();
        float waitTime = (float)(random.NextDouble() * 15.0 + 5.0);
        jumpTimer.WaitTime = waitTime;
        jumpTimer.Start();
    }

    private void OnJumpTimerTimeout()
    {
        Jump();
        StartJumpTimer();
    }

    private void OnBodyEntered(Node body)
    {
        if (body is Pp player)
        {
            overlappingPlayer = player;
            if (!player.IsInvincible())
                player.TakeDamage(1);
        }
    }

    private void OnBodyExited(Node body)
    {
        if (body == overlappingPlayer)
        {
            overlappingPlayer = null;
        }
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        GD.Print($"Vie restante de l'ennemi : {Health}");
        bossBar.Value = Health;

        if (Health <= 0)
            QueueFree();
    }
}
