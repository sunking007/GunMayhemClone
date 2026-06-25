// filepath: /run/media/sunkinglin/3899EA9A7C62F1F9/GunMayhemClone/Scripts/Weapon.cs
using Godot;
using System;

namespace GunMayhemClone;

public partial class Weapon : Node2D
{
	[ExportGroup("Weapon Configurations")]
	[Export] public float FireRateCooldown = 0.2f;
	[Export] public float KnockbackForce = 350.0f;
	[Export] public float BulletSpeed = 1200.0f;
	[Export] public PackedScene BulletScene;

	[ExportGroup("Ammo")]
	[Export] public int ClipSize = 8;
	[Export] public float ReloadDuration = 1.0f;

	[ExportGroup("Physics Weapon Recoil")]
	[Export] public float PlayerPushbackForce = 250.0f;

	[ExportGroup("Muzzle Flash Settings")]
	[Export] public float FlashDuration = 0.05f;

	[ExportGroup("Sound")]
	[Export] public AudioStream ShootSound;
	[Export] public float ShootVolumeDb = 0.0f;
	private Marker2D _muzzle;
	private Sprite2D _muzzleFlash;
	private float _cooldownTimer = 0.0f;
	private float _flashTimer = 0.0f;
	private float _reloadTimer = 0.0f;
	private bool _isReloading = false;
	private int _ammoInClip;

	private CharacterBody2D _myPlayer;

	public event Action<int, int, bool> AmmoChanged;

	public int AmmoInClip => _ammoInClip;
	public int ClipCapacity => ClipSize;
	public bool IsReloading => _isReloading;

	public override void _Ready()
	{
		_muzzle = GetNodeOrNull<Marker2D>("Muzzle");
		_muzzleFlash = GetNodeOrNull<Sprite2D>("Muzzelflash");
		_myPlayer = GetNodeOrNull<CharacterBody2D>("../..");

		if (_muzzleFlash != null) _muzzleFlash.Visible = false;

		_ammoInClip = ClipSize;
		NotifyAmmoChanged();
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

		if (_isReloading)
		{
			_reloadTimer -= (float)delta;
			if (_reloadTimer <= 0.0f)
			{
				CompleteReload();
			}
		}
	}

	public bool PullTrigger(float facingDirection)
	{
		if (_cooldownTimer > 0.0f || BulletScene == null || _muzzle == null)
			return false;

		if (_isReloading)
			return false;

		if (_ammoInClip <= 0)
		{
			StartReload();
			return false;
		}

		_cooldownTimer = FireRateCooldown;
		_ammoInClip--;

		if (_muzzleFlash != null)
		{
			_muzzleFlash.Visible = true;
			_flashTimer = FlashDuration;
			_muzzleFlash.FlipV = GD.Randf() > 0.5f;
		}

		if (_myPlayer is Player playerEntity)
		{
			playerEntity.ApplyWeaponRecoil(-facingDirection, PlayerPushbackForce);
		}

		Bullet newBullet = BulletScene.Instantiate<Bullet>();
		GetTree().Root.AddChild(newBullet);
		newBullet.Launch(_muzzle.GlobalPosition, facingDirection, BulletSpeed, KnockbackForce, _myPlayer);

		// Play shooting sound at muzzle position
		if (ShootSound != null)
		{
			PlaySoundAtPosition(ShootSound, _muzzle.GlobalPosition, ShootVolumeDb);
		}

		if (_ammoInClip <= 0)
		{
			StartReload();
		}

		NotifyAmmoChanged();
		return true;
	}

	private void StartReload()
	{
		if (_isReloading || ClipSize <= 0)
			return;
		_isReloading = true;
		_reloadTimer = ReloadDuration;
		_cooldownTimer = 0.0f;

		if (_muzzleFlash != null)
			_muzzleFlash.Visible = false;

		NotifyAmmoChanged();
	}

	private void CompleteReload()
	{
		_isReloading = false;
		_ammoInClip = ClipSize;
		_reloadTimer = 0.0f;
		NotifyAmmoChanged();
	}

	private void NotifyAmmoChanged()
	{
		AmmoChanged?.Invoke(_ammoInClip, ClipSize, _isReloading);
	}

	// Helper: create an AudioStreamPlayer2D, play and free when finished
	private async void PlaySoundAtPosition(AudioStream stream, Vector2 globalPos, float volumeDb = 0.0f)
	{
		if (stream == null) return;
		var player = new AudioStreamPlayer2D();
		player.Stream = stream;
		player.GlobalPosition = globalPos;
		player.VolumeDb = volumeDb;
		GetTree().Root.AddChild(player);
		player.Play();
		// wait until not playing
		while (player.Playing)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		player.QueueFree();
	}
}
