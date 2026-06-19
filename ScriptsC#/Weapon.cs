using Godot;
using System;

namespace GunMayhemClone;

public partial class Weapon : Polygon2D
{
	[ExportGroup("Weapon Configurations")]
	[Export] public float FireRateCooldown = 0.2f; 
	[Export] public float KnockbackForce = 350.0f;  
	
	// Link your visual_tracer.tscn file here inside the Godot Inspector!
	[Export] public PackedScene VisualTracerScene;

	private RayCast2D _hitScanner;
	private Marker2D _muzzle;
	private float _cooldownTimer = 0.0f;

	public override void _Ready()
	{
		_hitScanner = GetNodeOrNull<RayCast2D>("HitScanner");
		_muzzle = GetNodeOrNull<Marker2D>("Muzzle");

		// CRITICAL FIX: Find the grandparent player node holding this weapon
		CharacterBody2D myPlayer = GetNodeOrNull<CharacterBody2D>("../..");
		if (_hitScanner != null && myPlayer != null)
		{
			// Tell the math raycast to completely ignore the player who pulled the trigger!
			_hitScanner.AddException(myPlayer);
		}
	}

	public override void _Process(double delta)
	{
		if (_cooldownTimer > 0)
		{
			_cooldownTimer -= (float)delta;
		}
	}

	public void PullTrigger(float facingDirection)
	{
		if (_cooldownTimer > 0 || _hitScanner == null || _muzzle == null) return;

		_cooldownTimer = FireRateCooldown;

		// 1. Force instant mathematical line calculations right now
		// FIX: Removed manual TargetPosition multiplication because GunPivot handle scale turns natively!
		_hitScanner.ForceRaycastUpdate();

		// Default travel length across the screen area if nothing is hit
		float visualFlyDistance = 1500.0f;

		// 2. Logic & Hit Check (Now safely ignores yourself)
		if (_hitScanner.IsColliding())
		{
			Vector2 collisionPoint = _hitScanner.GetCollisionPoint();
			
			// Calculate distance from barrel to the target hit point
			visualFlyDistance = _muzzle.GlobalPosition.DistanceTo(collisionPoint);

			GodotObject collider = _hitScanner.GetCollider();
			if (collider is Player hitPlayer)
			{
				GD.Print($"Instant Raycast calculation hit: {hitPlayer.Name}!");
				// Apply the physical pushback force velocity spike to the target player
				hitPlayer.ApplyKnockback(facingDirection, KnockbackForce);
			}
		}

		// 3. Visual Layer: Spawn our moving fake bullet tracer into the world scene layer
		if (VisualTracerScene != null)
		{
			VisualTracer tracerInstance = VisualTracerScene.Instantiate<VisualTracer>();
			
			tracerInstance.GlobalPosition = _muzzle.GlobalPosition;
			tracerInstance.Direction = new Vector2(facingDirection, 0.0f);
			tracerInstance.MaxDistance = visualFlyDistance; // Cut bullet trail off exactly at impact location
			
			GetTree().CurrentScene.AddChild(tracerInstance);
		}
	}
}
