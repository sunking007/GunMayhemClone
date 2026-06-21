using Godot;
using System;

namespace GunMayhemClone;

public partial class Weapon : Sprite2D
{
	[ExportGroup("Weapon Configurations")]
	[Export] public float FireRateCooldown = 0.2f; 
	[Export] public float KnockbackForce = 350.0f;  
	[Export] public float BulletSpeed = 1200.0f; 
	[Export] public PackedScene BulletScene; 

	private Marker2D _muzzle;
	private float _cooldownTimer = 0.0f;
	private CharacterBody2D _myPlayer;

	public override void _Ready()
	{
		_muzzle = GetNodeOrNull<Marker2D>("Muzzle");
		_myPlayer = GetNodeOrNull<CharacterBody2D>("../..");
	}

	public override void _Process(double delta)
	{
		if (_cooldownTimer > 0.0f)
		{
			_cooldownTimer -= (float)delta;
		}
	}

	public void PullTrigger(float facingDirection)
	{
		if (_cooldownTimer > 0.0f || BulletScene == null || _muzzle == null) return;

		_cooldownTimer = FireRateCooldown;

		Bullet newBullet = BulletScene.Instantiate<Bullet>();
		GetTree().Root.AddChild(newBullet);
		newBullet.Launch(_muzzle.GlobalPosition, facingDirection, BulletSpeed, KnockbackForce, _myPlayer);
	}
}
