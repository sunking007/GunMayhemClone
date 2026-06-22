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

	[ExportGroup("Physics Weapon Recoil")]
	[Export] public float PlayerPushbackForce = 250.0f; // Force pushing the PLAYER backward

	[ExportGroup("Muzzle Flash Settings")]
	[Export] public float FlashDuration = 0.05f;

	private Marker2D _muzzle;
	private Sprite2D _muzzleFlash;
	private float _cooldownTimer = 0.0f;
	private float _flashTimer = 0.0f;
	private CharacterBody2D _myPlayer;

	public override void _Ready()
	{
		_muzzle = GetNodeOrNull<Marker2D>("Muzzle");
		_muzzleFlash = GetNodeOrNull<Sprite2D>("Muzzelflash");
		_myPlayer = GetNodeOrNull<CharacterBody2D>("../..");

		if (_muzzleFlash != null) _muzzleFlash.Visible = false;
	}

	public override void _Process(double delta)
	{
		if (_cooldownTimer > 0.0f)
		{
			_cooldownTimer -= (float)delta;
		}

		if (_flashTimer > 0.0f)
		{
			_flashTimer -= (float)delta;
			if (_flashTimer <= 0.0f && _muzzleFlash != null)
			{
				_muzzleFlash.Visible = false;
			}
		}
	}

	public void PullTrigger(float facingDirection)
	{
		if (_cooldownTimer > 0.0f || BulletScene == null || _muzzle == null) return;

		_cooldownTimer = FireRateCooldown;

		if (_muzzleFlash != null)
		{
			_muzzleFlash.Visible = true;
			_flashTimer = FlashDuration;
			
			// FIXED TYPO HERE: FlipV or FlipH properties use direct assignment (=), not a function call ()
			_muzzleFlash.FlipV = GD.Randf() > 0.5f;
		}

		// Inject physical recoil into the player character
		// Firing RIGHT (1.0f) applies recoil to the LEFT (-1.0f)
		if (_myPlayer is Player playerEntity)
		{
			playerEntity.ApplyWeaponRecoil(-facingDirection, PlayerPushbackForce);
		}

		Bullet newBullet = BulletScene.Instantiate<Bullet>();
		GetTree().Root.AddChild(newBullet);
		newBullet.Launch(_muzzle.GlobalPosition, facingDirection, BulletSpeed, KnockbackForce, _myPlayer);
	}
}
