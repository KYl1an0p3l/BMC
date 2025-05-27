using Godot;
using System;

public partial class RifleProjectile : Area2D
{
    [Export] public float Speed = 1500f;
    public Vector2 Direction = Vector2.Right;
    public int Damage = 1;

    public override void _Ready()
    {
        CollisionMask = (1 << 0) | (1 << 2); // Mur (1) + Ennemi (3)
        Monitoring = true;
        Monitorable = true;

        if (Direction == Vector2.Up)
            RotationDegrees = -90;
        else if (Direction == Vector2.Down)
            RotationDegrees = 90;
        else if (Direction == Vector2.Left)
            RotationDegrees = 180;
        else
            RotationDegrees = 0; // Droite par défaut
            
        Connect("body_entered", new Callable(this, nameof(OnBodyEntered)));

    }

    public override void _PhysicsProcess(double delta)
    {
        Position += Direction * Speed * (float)delta;
    }

    private void OnBodyEntered(Node body)
    {
        GD.Print($"Projectile a touché : {body.Name}");

        if (body is Enemy1 || body is Enemy2 || body is BossTest)
        {
            GD.Print("→ Ennemi touché");

            if (body is Enemy1 enemy1)
                enemy1.TakeDamage(Damage);
            else if (body is Enemy2 enemy2)
                enemy2.TakeDamage(Damage);
            else if (body is BossTest boss)
                boss.TakeDamage(Damage);
        }

        // Dans tous les cas, on détruit le projectile après collision
        QueueFree();
    }
}
