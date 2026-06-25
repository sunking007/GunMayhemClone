// filepath: /run/media/sunkinglin/3899EA9A7C62F1F9/GunMayhemClone/Scripts/Player.cs
using Godot;
using System;

namespace GunMayhemClone;

public partial class Player : CharacterBody2D
{
	[ExportGroup("Setup")]
	[Export] public string InputPrefix = "p1_";
	[Export] public PackedScene DustEffectScene;
	[Export] public PlayerUiCard UiCard;

	[ExportGroup("Horizontal Movement")]
	[Export] public float Speed = 450.0f;

	[ExportGroup("Advanced Jump Settings")]
	[Export] public float MaxJumpHeight = 150.0f;
	[Export] public float MinJumpHeight = 40.0f;
	[Export] public float TimeToPeak = 0.4f;
	[Export] public float TimeToFall = 0.3f;
	[Export] public int MaxJumps = 2;

	[ExportGroup("Traditional Health System")]
	[Export] public float MaxHealth = 100.0f;
	[Export] public int Lives = 3;
	[Export] public NodePath TargetSpawnPointNode;

	[ExportGroup("Knockback Scaling Matrix")]
	[Export] public float KnockbackScaleFactor = 1.0f;

	[ExportGroup("Sound")]
	[Export] public AudioStream DustSound1;
	[Export] public AudioStream DustSound2;
	[Export] public AudioStream DustSound3;
	[Export] public float DustVolumeDb = -2.0f;

	private string _actionLeft;
	private string _actionRight;
	private string _actionJump;
	private string _actionDown;
	private string _actionShoot;

	private float _jumpVelocity;
	private float _minJumpVelocity;
	private float _gravity;
	private float _fallGravity;
	private int _jumpCount = 0;

	private Vector2 _knockbackVelocity = Vector2.Zero;
	private float _lastValidDirection = 1.0f;
	private bool _wasOnFloorLastFrame = true;

	private Sprite2D _body;
	private Sprite2D _head;
	private Marker2D _gunPivot;
	private Weapon _currentWeapon;
	private Label _ammoLabel;

	private float _currentHealth;
	private Node2D _spawnMarker;

	private bool _isDead = false;

	private AnimationPlayer _animationPlayer;
	private bool _isShooting = false;

	public override void _Ready()
	{
		_body = GetNodeOrNull<Sprite2D>("Body");
		_head = GetNodeOrNull<Sprite2D>("head");
		_gunPivot = GetNodeOrNull<Marker2D>("GunPivot");
		_currentWeapon = GetNodeOrNull<Weapon>("GunPivot/Weapon");
		_ammoLabel = GetNodeOrNull<Label>("AmmoLabel");

		_animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (_animationPlayer != null)
		{
			_animationPlayer.AnimationFinished += OnAnimationFinished;
			_animationPlayer.Play("idle");
		}

		if (TargetSpawnPointNode != null)
		{
			_spawnMarker = GetNodeOrNull<Node2D>(TargetSpawnPointNode);
		}

		_actionLeft = InputPrefix + "move_left";
		_actionRight = InputPrefix + "move_right";
		_actionJump = InputPrefix + "jump";
		_actionDown = InputPrefix + "move_down";
		_actionShoot = InputPrefix + "shoot";

		CalculateJumpParameters();
		AddToGroup("players");

		_currentHealth = MaxHealth;

		if (_spawnMarker != null)
		{
			GlobalPosition = _spawnMarker.GlobalPosition;
		}

		if (UiCard != null)
		{
			UiCard.SetupCard(Name, null, MaxHealth, Lives);
		}

		if (_currentWeapon != null)
		{
			_currentWeapon.AmmoChanged += OnWeaponAmmoChanged;
			UpdateAmmoDisplay(_currentWeapon.AmmoInClip, _currentWeapon.ClipCapacity, _currentWeapon.IsReloading);
		}
		else
		{
			UpdateAmmoDisplay(0, 0, false);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;

		Vector2 velocity = Velocity;

		if (!IsOnFloor())
		{
			float currentGravity = (velocity.Y > 0) ? _fallGravity : _gravity;
			velocity.Y += currentGravity * (float)delta;
		}
		else
		{
			_jumpCount = 0;
		}

		if (Input.IsActionJustPressed(_actionJump) && (IsOnFloor() || _jumpCount < MaxJumps))
		{
			velocity.Y = _jumpVelocity;
			_jumpCount++;
		}
		else if (Input.IsActionJustReleased(_actionJump) && velocity.Y < _minJumpVelocity)
		{
			velocity.Y = _minJumpVelocity;
		}

		if (IsOnFloor() && Input.IsActionJustPressed(_actionDown))
		{
			DropThroughPlatform();
		}

		float direction = 0.0f;
		bool pressingLeft = Input.IsActionPressed(_actionLeft);
		bool pressingRight = Input.IsActionPressed(_actionRight);

		if (pressingLeft && pressingRight)
		{
			if (Input.IsActionJustPressed(_actionLeft)) direction = -1.0f;
			else if (Input.IsActionJustPressed(_actionRight)) direction = 1.0f;
			else direction = _lastValidDirection;
		}
		else if (pressingLeft) direction = -1.0f;
		else if (pressingRight) direction = 1.0f;

		float targetWalkingVelocity = 0.0f;
		if (direction != 0)
		{
			targetWalkingVelocity = direction * Speed;
			_lastValidDirection = direction;
			FlipCharacter(direction);
		}
		else
		{
			targetWalkingVelocity = Mathf.MoveToward(velocity.X - _knockbackVelocity.X, 0, Speed * 0.25f);
		}

		_knockbackVelocity.X = Mathf.MoveToward(_knockbackVelocity.X, 0.0f, 1200.0f * (float)delta);
		_knockbackVelocity.Y = Mathf.MoveToward(_knockbackVelocity.Y, 0.0f, 1200.0f * (float)delta);

		velocity.X = targetWalkingVelocity + _knockbackVelocity.X;

		if (Mathf.Abs(_knockbackVelocity.Y) > 0.1f)
		{
			velocity.Y += _knockbackVelocity.Y;
			_knockbackVelocity.Y = 0;
		}

		if (Input.IsActionPressed(_actionShoot) && _currentWeapon != null)
		{
			_currentWeapon.PullTrigger(_lastValidDirection);

			if (_animationPlayer != null)
			{
				_isShooting = true;
				_animationPlayer.Stop();
				_animationPlayer.Play("shoot");
			}
		}

		Velocity = velocity;
		MoveAndSlide();

		if (IsOnFloor() && !_wasOnFloorLastFrame)
		{
			SpawnDustCloud();
		}

		_wasOnFloorLastFrame = IsOnFloor();
	}

	private void OnWeaponAmmoChanged(int ammo, int clipSize, bool isReloading)
	{
		UpdateAmmoDisplay(ammo, clipSize, isReloading);
	}

	private void UpdateAmmoDisplay(int ammo, int clipSize, bool isReloading)
	{
		if (_ammoLabel == null) return;

		// show only the current ammo count (no "Ammo:" prefix)
		if (isReloading)
		{
			_ammoLabel.Text = "0";
		}
		else
		{
			_ammoLabel.Text = ammo.ToString();
		}
	}

	public void ApplyKnockback(float horizontalDirection, float force)
	{
		if (_isDead) return;
		Vector2 knockbackVector = new Vector2(horizontalDirection, -0.3f).Normalized();
		_knockbackVelocity += knockbackVector * force;
	}

	public void ApplyWeaponRecoil(float horizontalDirection, float force)
	{
		if (_isDead) return;
		_knockbackVelocity.X += horizontalDirection * force;
	}

	public void ReceiveHit(float horizontalDirection, float damageAmount, float baseForce)
	{
		if (_isDead) return;

		_currentHealth -= damageAmount;
		GD.Print($"{Name} Hit! HP remaining: {_currentHealth}/{MaxHealth}");

		if (UiCard != null)
		{
			UiCard.UpdateHealthDisplay(_currentHealth);
		}

		if (_currentHealth <= 0.0f)
		{
			HandleDeath();
			return;
		}

		float amplifiedForce = baseForce * KnockbackScaleFactor;
		ApplyKnockback(horizontalDirection, amplifiedForce);
	}

	private void HandleDeath()
	{
		Lives--;
		GD.Print($"{Name} Died! Remaining Lives: {Lives}");

		if (UiCard != null)
		{
			UiCard.UpdateLivesDisplay(Lives);
		}

		if (Lives > 0)
		{
			if (_spawnMarker != null) GlobalPosition = _spawnMarker.GlobalPosition;

			Velocity = Vector2.Zero;
			_knockbackVelocity = Vector2.Zero;
			_currentHealth = MaxHealth;

			if (UiCard != null)
			{
				UiCard.UpdateHealthDisplay(_currentHealth);
			}

			if (_animationPlayer != null)
			{
				_isShooting = false;
				_animationPlayer.Play("idle");
			}
		}
		else
		{
			GD.Print($"{Name} Out of lives! Eliminated!");
			QueueFree();
		}
	}

	public bool ProcessFallingLifePenalty()
	{
		if (_isDead) return Lives > 0;
		_isDead = true;

		Lives--;
		GD.Print($"{Name} fell out of bounds! Remaining Lives: {Lives}");

		if (UiCard != null)
		{
			UiCard.UpdateLivesDisplay(Lives);
		}

		if (Lives > 0)
		{
			_currentHealth = MaxHealth;
			Velocity = Vector2.Zero;
			_knockbackVelocity = Vector2.Zero;

			this.SetCollisionLayerValue(2, false);
			this.SetCollisionMaskValue(1, false);

			if (_spawnMarker != null)
			{
				GlobalPosition = _spawnMarker.GlobalPosition;
			}

			if (UiCard != null)
			{
				UiCard.UpdateHealthDisplay(_currentHealth);
			}

			return true;
		}
		else
		{
			GD.Print($"{Name} out of lives! Eliminating player.");
			QueueFree();
			return false;
		}
	}

	public void CompleteRespawn()
	{
		_isDead = false;
		Velocity = Vector2.Zero;
		_knockbackVelocity = Vector2.Zero;

		this.SetCollisionLayerValue(2, true);
		this.SetCollisionMaskValue(1, true);

		if (_animationPlayer != null)
		{
			_isShooting = false;
			_animationPlayer.Play("idle");
		}
	}

	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "shoot")
		{
			_isShooting = false;
			_animationPlayer.Play("idle");
		}
	}

	private void SpawnDustCloud()
	{
		if (DustEffectScene == null) return;
		var dustInstance = DustEffectScene.Instantiate<GpuParticles2D>();
		GetParent().AddChild(dustInstance);
		dustInstance.GlobalPosition = new Vector2(GlobalPosition.X, GlobalPosition.Y + 45.0f);
		dustInstance.Emitting = true;

		// Play one of the three dust sounds at random only when dust spawn happens
		AudioStream chosen = null;
		float vol = DustVolumeDb;
		float r = GD.Randf();
		if (r < 0.34f) chosen = DustSound1;
		else if (r < 0.67f) chosen = DustSound2;
		else chosen = DustSound3;

		if (chosen != null)
		{
			PlaySoundAtPosition(chosen, dustInstance.GlobalPosition, vol);
		}
	}

	private async void PlaySoundAtPosition(AudioStream stream, Vector2 globalPos, float volumeDb = 0.0f)
	{
		if (stream == null) return;
		var player = new AudioStreamPlayer2D();
		player.Stream = stream;
		player.GlobalPosition = globalPos;
		player.VolumeDb = volumeDb;
		GetTree().Root.AddChild(player);
		player.Play();
		while (player.Playing)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		player.QueueFree();
	}

	private async void DropThroughPlatform()
	{
		this.SetCollisionMaskValue(1, false);
		for (int i = 0; i < 6; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		this.SetCollisionMaskValue(1, true);
	}

	private void CalculateJumpParameters()
	{
		_gravity = (2.0f * MaxJumpHeight) / Mathf.Pow(TimeToPeak, 2.0f);
		_fallGravity = (2.0f * MaxJumpHeight) / Mathf.Pow(TimeToFall, 2.0f);
		_jumpVelocity = -((2.0f * MaxJumpHeight) / TimeToPeak);
		_minJumpVelocity = -Mathf.Sqrt(2.0f * _gravity * MinJumpHeight);
	}

	private void FlipCharacter(float direction)
	{
		bool isFlipped = direction < 0;
		if (_body != null) _body.FlipH = isFlipped;
		if (_head != null) _head.FlipH = isFlipped;

		if (_gunPivot != null)
		{
			Vector2 pivotScale = _gunPivot.Scale;
			pivotScale.X = isFlipped ? -Mathf.Abs(pivotScale.X) : Mathf.Abs(pivotScale.X);
			_gunPivot.Scale = pivotScale;
		}
	}
}
