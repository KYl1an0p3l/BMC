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
    private bool isAttacking = false;
    private bool isChargingDash = false;
    private float dashTimer = 0f;
    private float dashElapsed = 0f;
    private float dashMinTime = 0.2f;
    private Vector2 dashDirection = Vector2.Zero;
    private ProgressBar bossBar;
    private RayCast2D raycastWall;
    private RayCast2D raycastGround;
    private RayCast2D raycastWall2;
    private RayCast2D raycastGround2;
    private float detectionRadius = 0f;
    private float meleeRangeWidth = 0f;
    private bool playerInAttackZone = false;
    private Pp playerInAttack = null;
    private bool attackCoroutineRunning = false;
    private RayCast2D aimRay;
    private Random random = new Random();
    private bool isShootingSequenceRunning = false;
    private bool isPlayerInUpShotZone = false;
    private Pp playerInUpShotZone = null;
    private bool upShotCoroutineRunning = false;

    private string currentDirection = "right";
    private string dashDirectionName = "right"; // Added to store dash direction

    public override void _Ready()
    {
        maxHealth = Health;
        Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        player = GetTree().GetFirstNodeInGroup("player") as Pp;
        raycastWall = GetNode<RayCast2D>("RaycastWall");
        raycastGround = GetNode<RayCast2D>("RaycastGround");
        raycastWall2 = GetNode<RayCast2D>("RaycastWall2");
        raycastGround2 = GetNode<RayCast2D>("RaycastGround2");
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

        var Up_Shot = GetNode<Area2D>("Up_Shot");
        Up_Shot.BodyEntered += OnUpShotZoneEntered;
        Up_Shot.BodyExited += OnUpShotZoneExited;

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
        if (player == null || !GodotObject.IsInstanceValid(player))
        {
            player = GetTree().GetFirstNodeInGroup("player") as Pp;
            if (player == null) return;
        }

        if (isAttacking)
        {
            velocity = Vector2.Zero;
            Velocity = velocity;
            MoveAndSlide();
            return;
        }
        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);

        if (!IsOnFloor())
            velocity.Y += Gravity * (float)delta;
        else
            velocity.Y = 0;

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= (float)delta;

        if (isDashing)
        {
            dashTimer -= (float)delta;
            dashElapsed += (float)delta;

            // Fin du dash si le temps est écoulé
            if (dashTimer <= 0f)
            {
                GD.Print("Fin du dash (temps écoulé)");
                isDashing = false;
                velocity = Vector2.Zero;
                Velocity = velocity;
                MoveAndSlide();
                return;
            }

            bool goingLeft = dashDirection.X < 0;
            currentDirection = goingLeft ? "left" : "right";

            if (goingLeft)
            {
                raycastWall2.ForceRaycastUpdate();
                raycastGround2.ForceRaycastUpdate();

                if (dashElapsed >= dashMinTime && (raycastWall2.IsColliding() || !raycastGround2.IsColliding()))
                {
                    isDashing = false;
                    velocity = Vector2.Zero;
                }
                else
                {
                    velocity.X = dashDirection.X * DashSpeed;
                }
            }
            else
            {
                raycastWall.ForceRaycastUpdate();
                raycastGround.ForceRaycastUpdate();

                if (dashElapsed >= dashMinTime && (raycastWall.IsColliding() || !raycastGround.IsColliding()))
                {
                    isDashing = false;
                    velocity = Vector2.Zero;
                }
                else
                {
                    velocity.X = dashDirection.X * DashSpeed;
                }
            }

            Velocity = velocity;
            MoveAndSlide();
            Sprite.Play("dash_" + dashDirectionName);
            return;
        }


        Aim();

        if (!isDashing && !isChargingDash)
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
            else if (distance >= detectionRadius && dashCooldownTimer <= 0 && !isShootingSequenceRunning && IsOnFloor())
            {
                _ = ShootTwiceThenDash();
            }
        }

        Velocity = velocity;
        MoveAndSlide();

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (isPlayerInUpShotZone)
            return;

        if (isAttacking)
            return;

        if (isChargingDash)
        {
            // On utilise la direction actuelle du joueur
            currentDirection = player.GlobalPosition.X < GlobalPosition.X ? "left" : "right";
            Sprite.Play("charge_dash_" + currentDirection);
            return;
        }

        if (isDashing)
        {
            // On utilise la direction mémorisée au début du dash
            Sprite.Play("dash_" + dashDirectionName);
            return;
        }

        // Direction normale
        if (Mathf.Abs(player.GlobalPosition.X - GlobalPosition.X) > 2f) // tolérance de 10 pixels
        {
            currentDirection = player.GlobalPosition.X < GlobalPosition.X ? "left" : "right";
        }


        if (Mathf.Abs(velocity.X) > 1.0f)
            Sprite.Play(currentDirection);
        else
            Sprite.Play("idle_" + currentDirection);
    }

    private async Task ShootTwiceThenDash()
    {
        isShootingSequenceRunning = true;
        Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
        Shoot(direction);
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
        Shoot(direction);
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");
        StartDashSequence();
        isShootingSequenceRunning = false;
    }

    private void FollowPlayer()
    {
        if (player != null && GodotObject.IsInstanceValid(player))
        {
            bool goingLeft = player.GlobalPosition.X < GlobalPosition.X;
            velocity.X = goingLeft ? -Speed : Speed;
            UpdateRaycasts(goingLeft);
        }
    }

    private void UpdateRaycasts(bool left)
    {
        if (left)
        {
            raycastWall2.Enabled = true;
            raycastGround2.Enabled = true;
            raycastWall.Enabled = false;
            raycastGround.Enabled = false;
        }
        else
        {
            raycastWall.Enabled = true;
            raycastGround.Enabled = true;
            raycastWall2.Enabled = false;
            raycastGround2.Enabled = false;
        }

        Vector2 wallTarget = new Vector2(left ? -120 : 120, 0);
        Vector2 groundTarget = new Vector2(left ? -100 : 100, 110);

        if (left)
        {
            raycastWall2.TargetPosition = wallTarget;
            raycastGround2.TargetPosition = groundTarget;
        }
        else
        {
            raycastWall.TargetPosition = wallTarget;
            raycastGround.TargetPosition = groundTarget;
        }
    }

    private async void StartDashSequence()
    {
        if (player == null || !GodotObject.IsInstanceValid(player)) return;
        if (!IsOnFloor()) return;

        isChargingDash = true;
        dashCooldownTimer = DashCooldown;
        velocity = Vector2.Zero;
        Velocity = velocity;
        MoveAndSlide();

        // → Animation de charge
        string chargeAnim = player.GlobalPosition.X < GlobalPosition.X ? "charge_dash_left" : "charge_dash_right";
        Sprite.Play(chargeAnim);

        await ToSignal(GetTree().CreateTimer(DashChargeTime), "timeout");

        if (player == null || !GodotObject.IsInstanceValid(player)) return;

        dashDirection = (player.GlobalPosition - GlobalPosition).Normalized();
        dashDirectionName = dashDirection.X < 0 ? "left" : "right"; // ← mémorise la direction
        isChargingDash = false;
        isDashing = true;
        dashTimer = DashDuration;
        dashElapsed = 0f;
    }

    private async void MeleeAttack()
    {
        if (isAttacking) return;

        GD.Print("Boss attaque au corps à corps !");
        isAttacking = true;

        string atkAnim = player.GlobalPosition.X < GlobalPosition.X ? "atk_left" : "atk_right";
        Sprite.Play(atkAnim);

        velocity = Vector2.Zero;
        Velocity = velocity;
        MoveAndSlide();

        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

        isAttacking = false;
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
            await ToSignal(GetTree().CreateTimer(0.6f), "timeout");
            if (!playerInAttackZone || !IsInstanceValid(playerInAttack)) break;

            while (playerInAttack != null && IsInstanceValid(playerInAttack) && playerInAttack.IsInvincible())
            {
                await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
                if (!playerInAttackZone || !IsInstanceValid(playerInAttack)) break;
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

        if (bullet is Spike bulletScript)
            bulletScript.SetDirection(direction);

        GetTree().CurrentScene.AddChild(bullet);
    }

    private void OnUpShotZoneEntered(Node body)
    {
        if (body is Pp p)
        {
            playerInUpShotZone = p;
            isPlayerInUpShotZone = true;
            if (!upShotCoroutineRunning)
                _ = HandleUpShotZone();
        }
    }

    private void OnUpShotZoneExited(Node body)
    {
        if (body == playerInUpShotZone)
        {
            isPlayerInUpShotZone = false;
            playerInUpShotZone = null;
        }
    }

    private async Task HandleUpShotZone()
    {
        upShotCoroutineRunning = true;

        float timer = 0f;
        while (isPlayerInUpShotZone && IsInstanceValid(playerInUpShotZone))
        {
            await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
            timer += 0.1f;

            if (timer >= 1.0f)
            {
                GD.Print("Le boss tire après 2 secondes dans Up_Shot !");
                Vector2 direction = (playerInUpShotZone.GlobalPosition - GlobalPosition).Normalized();
                Shoot(direction);
                break;
            }
        }

        upShotCoroutineRunning = false;
    }
}
