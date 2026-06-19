using Godot;
using System;

namespace GunMayhemClone;

public partial class Bullet : CharacterBody2D
{
	private Vector2 _velocity;
	private float _knockbackForce;
	private float _direction;
	private GodotObject _shooter;

	public void Launch(Vector2 position, float facingDirection, float speed, float force, GodotObject shooterInstance)
	{
		this.GlobalPosition = position;
		_direction = facingDirection;
		_knockbackForce = force;
		_shooter = shooterInstance;
		_velocity = new Vector2(facingDirection * speed, 0.0f);
	}

	public override void _PhysicsProcess(double delta)
	{
		KinematicCollision2D collision = MoveAndCollide(_velocity * (float)delta);

		if (collision != null)
		{
			GodotObject target = collision.GetCollider();

			if (target == _shooter) return;

			if (target is Player hitPlayer)
			{
				hitPlayer.ApplyKnockback(_direction, _knockbackForce);
			}

			QueueFree();
		}
	}
}
