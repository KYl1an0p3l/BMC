using Godot;
using System;
using System.Threading.Tasks;

public partial class BossTest : CharacterBody2D
{
    [Export] public int Health = 30;
    [Export] public int Speed = 100;
    [Export] public float Gravity = 980f;
    [Export] public float DashSpeed = 800f;
    [Export] public float DashDuration = 1.0f;
    [Export] public float DashCooldown = 1.0f;
    [Export] public float DashChargeTime = 1.0f;
    [Export] public PackedScene ammo;

    private int maxHealth;
    private Vector2 velocity = Vector2.Zero;
    private AnimatedSprite2D Sprite;
    private Pp player;
    private Pp overlappingPlayer = null;
    private float dashCooldownTimer = 0f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector2 dashDirection = Vector2.Zero;
    private ProgressBar bossBar;
    private RayCast2D raycastWall;
    private RayCast2D raycastGround;
    private float detectionRadius = 0f;
    private float meleeRangeWidth = 0f;
    private bool playerInAttackZone = false;
    private Pp playerInAttack = null;
    private bool attackCoroutineRunning = false;
    private RayCast2D aimRay;
    private Random random = new Random();
    private bool isShootingSequenceRunning = false;

    public override void _Ready()
    {
        maxHealth = Health;
        Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        player = GetTree().GetFirstNodeInGroup("player") as Pp;
        raycastWall = GetNode<RayCast2D>("RaycastWall");
        raycastGround = GetNode<RayCast2D>("RaycastGround");
        bossBar = GetNode<ProgressBar>("BossBar");
        bossBar.MaxValue = maxHealth;
        bossBar.Value = Health;

        aimRay = GetNode<RayCast2D>("AimRay");

        var damageArea = GetNode<Area2D>("DamageArea");
        damageArea.BodyEntered += OnBodyEntered;
        damageArea.BodyExited += OnBodyExited;

        var attackArea = GetNode<Area2D>("Attaque");
        attackArea.BodyEntered += OnAttackZoneEntered;
        attackArea.BodyExited += OnAttackZoneExited;

        var detectionShape = GetNode<CollisionShape2D>("PlayerDetection/CollisionShape2D").Shape as CircleShape2D;
        if (detectionShape != null)
            detectionRadius = detectionShape.Radius;

        var meleeShape = GetNode<CollisionShape2D>("MeleeRange/CollisionShape2D").Shape as RectangleShape2D;
        if (meleeShape != null)
            meleeRangeWidth = meleeShape.Size.X / 2f;

        CollisionLayer = 1 << 2;
        CollisionMask = 1 << 0;
    }

    public override void _Process(double delta)
    {
        if (overlappingPlayer != null && GodotObject.IsInstanceValid(overlappingPlayer) && !overlappingPlayer.IsInvincible())
        {
            overlappingPlayer.TakeDamage(1);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (player == null || !GodotObject.IsInstanceValid(player)) return;

        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);

        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;
        else
            velocity.Y = 0;

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= (float)delta;

        if (player.GlobalPosition.X < GlobalPosition.X)
            Sprite.Play("left");
        else
            Sprite.Play("right");

        if (isDashing)
        {
            dashTimer -= (float)delta;
            if (dashTimer <= 0)
            {
                isDashing = false;
                velocity = Vector2.Zero;
            }
            else
            {
                raycastWall.ForceRaycastUpdate();
                raycastGround.ForceRaycastUpdate();
                if (raycastWall.IsColliding() || !raycastGround.IsColliding())
                {
                    isDashing = false;
                    velocity = Vector2.Zero;
                }
                else
                {
                    velocity = dashDirection * DashSpeed;
                    Velocity = velocity;
                    MoveAndSlide();
                    return;
                }
            }
        }
        
        Aim();
        if (!isDashing)
        {
            if (distance < meleeRangeWidth)
            {
                MeleeAttack();
                velocity.X = 0;
            }
            else if (distance < detectionRadius)
            {
                FollowPlayer();
            }
            else if (distance >= detectionRadius && dashCooldownTimer <= 0 && !isShootingSequenceRunning)
            {
                _ = ShootTwiceThenDash();
            }

            Velocity = velocity;
            MoveAndSlide();
        }
    }
    
    private async Task ShootTwiceThenDash()
    {
        isShootingSequenceRunning = true;

        Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();

        Shoot(direction);
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
        

        Shoot(direction);
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

        StartDashSequence();

        isShootingSequenceRunning = false;
    }


    private void FollowPlayer()
    {
        if (player != null && GodotObject.IsInstanceValid(player))
            velocity.X = player.GlobalPosition.X < GlobalPosition.X ? -Speed : Speed;
    }

    private async void StartDashSequence()
    {
        if (player == null || !GodotObject.IsInstanceValid(player)) return;

        dashCooldownTimer = DashCooldown;
        velocity = Vector2.Zero;
        Velocity = velocity;
        MoveAndSlide();

        await ToSignal(GetTree().CreateTimer(DashChargeTime), "timeout");

        if (player == null || !GodotObject.IsInstanceValid(player)) return;

        dashDirection = (player.GlobalPosition - GlobalPosition).Normalized();
        isDashing = true;
        dashTimer = DashDuration;
    }

    private void MeleeAttack()
    {
        GD.Print("Boss attaque au corps à corps !");
    }

    private void OnBodyEntered(Node body)
    {
        if (body is Pp p)
        {
            overlappingPlayer = p;
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

    private void OnAttackZoneEntered(Node body)
    {
        if (body is Pp p)
        {
            playerInAttack = p;
            playerInAttackZone = true;
            if (!attackCoroutineRunning)
                _ = HandleAttackZoneDamage();
        }
    }

    private void OnAttackZoneExited(Node body)
    {
        if (body == playerInAttack)
        {
            playerInAttackZone = false;
            playerInAttack = null;
        }
    }

    private async Task HandleAttackZoneDamage()
    {
        attackCoroutineRunning = true;

        while (playerInAttackZone && IsInstanceValid(playerInAttack))
        {
            await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

            if (!playerInAttackZone || !IsInstanceValid(playerInAttack))
                break;

            while (playerInAttack != null && IsInstanceValid(playerInAttack) && playerInAttack.IsInvincible())
            {
                await ToSignal(GetTree().CreateTimer(0.1f), "timeout");

                if (!playerInAttackZone || !IsInstanceValid(playerInAttack))
                    break;
            }

            if (playerInAttackZone && IsInstanceValid(playerInAttack) && !playerInAttack.IsInvincible())
            {
                playerInAttack.TakeDamage(1);
                GD.Print("Dégâts infligés après invincibilité.");
            }
        }

        attackCoroutineRunning = false;
    }
    private void Aim()
    {
        if (player == null || !GodotObject.IsInstanceValid(player)) return;
        Vector2 aimDirection = (player.GlobalPosition - GlobalPosition).Normalized();
        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
        aimRay.TargetPosition = aimDirection * distance;
    }
    private void Shoot(Vector2 direction)
    {
        if (ammo == null) return;

        GD.Print("Boss tire !");
        Node2D bullet = (Node2D)ammo.Instantiate();
        bullet.GlobalPosition = GlobalPosition;

        if (bullet is Bullets bulletScript)
            bulletScript.SetDirection(direction);

        GetTree().CurrentScene.AddChild(bullet);
    }

}
