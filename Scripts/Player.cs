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

	private float _currentHealth;
	private Node2D _spawnMarker;
	
	// Safety tracking variable
	private bool _isDead = false;

	// 🛠️ ANIMATION DRIVER ENGINE FIELDS
	private AnimationPlayer _animationPlayer;
	private bool _isShooting = false;

	public override void _Ready()
	{
		_body = GetNodeOrNull<Sprite2D>("Body");
		_head = GetNodeOrNull<Sprite2D>("head");
		_gunPivot = GetNodeOrNull<Marker2D>("GunPivot");
		_currentWeapon = GetNodeOrNull<Weapon>("GunPivot/Weapon");

		// 🛠️ FETCH ANIMATIONPLAYER AND CONNECT COMPLETED SIGNAL LOOP HOOK
		_animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
		if (_animationPlayer != null)
		{
			_animationPlayer.AnimationFinished += OnAnimationFinished;
			_animationPlayer.Play("idle"); // Automatically kickstart your 1.2s looping hover
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
		this.AddToGroup("players");

		_currentHealth = MaxHealth;

		if (_spawnMarker != null)
		{
			GlobalPosition = _spawnMarker.GlobalPosition;
		}

		if (UiCard != null)
		{
			UiCard.SetupCard(Name, null, MaxHealth, Lives);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// Completely stop processing inputs and gravity loops if the player is dead
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

		if (Input.IsActionPressed(_actionShoot))
		{
			if (_currentWeapon != null)
			{
				_currentWeapon.PullTrigger(_lastValidDirection);
			}

			// 🛠️ SNAPPY TRACK RESTART FOR REPEATED BULLET TRIGGERS
			if (_animationPlayer != null)
			{
				_isShooting = true;
				_animationPlayer.Stop(); // Resets layout timeline instantly back to 0.0 seconds
				_animationPlayer.Play("shoot"); // Force dynamic layout snap
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

			// Force back to base animation loop upon life respawning
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

	// 🛠️ HOOK FOR DEATHZONE: Subtracts a single life and disables collisions to stop the double-hit bug
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
			
			// Force physical server collision box to turn off instantly
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

	// Re-enables input movement and physics collision layers after the timer delay ends
	public void CompleteRespawn()
	{
		_isDead = false;
		Velocity = Vector2.Zero;
		_knockbackVelocity = Vector2.Zero;

		// Turn physics collision box back on safely
		this.SetCollisionLayerValue(2, true);
		this.SetCollisionMaskValue(1, true);

		// Safely snap back to tracking idle configuration properties
		if (_animationPlayer != null)
		{
			_isShooting = false;
			_animationPlayer.Play("idle");
		}
	}

	// 🛠️ SAFE SYSTEM BRIDGE INTERPOLATION BACK TO THE 1.2S HOVER LOOP
	private void OnAnimationFinished(StringName animName)
	{
		if (animName == "shoot")
		{
			_isShooting = false;
			_animationPlayer.Play("idle"); // Runs your 1.2s hover infinitely until next trigger click
		}
	}

	private void SpawnDustCloud()
	{
		if (DustEffectScene == null) return;
		var dustInstance = DustEffectScene.Instantiate<GpuParticles2D>();
		GetParent().AddChild(dustInstance);
		dustInstance.GlobalPosition = new Vector2(GlobalPosition.X, GlobalPosition.Y + 45.0f);
		dustInstance.Emitting = true;
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

	// 🛠️ ENHANCED FLIPPING SYSTEM ARCHITECTURE: Prevents hands and weapon markers from drifting apart
	private void FlipCharacter(float direction)
	{
		bool isFlipped = direction < 0;
		if (_body != null) _body.FlipH = isFlipped;
		if (_head != null) _head.FlipH = isFlipped;

		// Flip your GunPivot and arms layout globally by modifying its horizontal scale container
		if (_gunPivot != null)
		{
			Vector2 pivotScale = _gunPivot.Scale;
			if (isFlipped)
			{
				pivotScale.X = -Mathf.Abs(pivotScale.X);
			}
			else
			{
				pivotScale.X = Mathf.Abs(pivotScale.X);
			}
			_gunPivot.Scale = pivotScale;
		}
	}
}
