using Godot;
using System;

namespace GunMayhemClone;

public partial class Player : CharacterBody2D
{
	[ExportGroup("Setup")]
	[Export] public string InputPrefix = "p1_"; 

	[ExportGroup("Horizontal Movement")]
	[Export] public float Speed = 450.0f;

	[ExportGroup("Advanced Jump Settings")]
	[Export] public float MaxJumpHeight = 150.0f; 
	[Export] public float MinJumpHeight = 40.0f;  
	[Export] public float TimeToPeak = 0.4f;      
	[Export] public float TimeToFall = 0.3f;      
	[Export] public int MaxJumps = 2;

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

	// Updated fields to use Sprite2D architecture
	private Sprite2D _body;
	private Sprite2D _head;
	private Marker2D _gunPivot;
	private Weapon _currentWeapon;

	public override void _Ready()
	{
		// Safely fetching the newly typed Sprite2D nodes
		_body = GetNodeOrNull<Sprite2D>("Body");
		_head = GetNodeOrNull<Sprite2D>("head");
		_gunPivot = GetNodeOrNull<Marker2D>("GunPivot");
		_currentWeapon = GetNodeOrNull<Weapon>("GunPivot/Weapon");

		_actionLeft = InputPrefix + "move_left";
		_actionRight = InputPrefix + "move_right";
		_actionJump = InputPrefix + "jump";
		_actionDown = InputPrefix + "move_down";
		_actionShoot = InputPrefix + "shoot";

		CalculateJumpParameters();
		this.AddToGroup("players");
	}

	public override void _PhysicsProcess(double delta)
	{
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
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public void ApplyKnockback(float horizontalDirection, float force)
	{
		Vector2 knockbackVector = new Vector2(horizontalDirection, -0.3f).Normalized();
		_knockbackVelocity += knockbackVector * force;
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
		
		// Use FlipH for clean visual sprite mirroring
		if (_body != null) _body.FlipH = isFlipped;
		if (_head != null) _head.FlipH = isFlipped;
		
		// Scale is kept on the gun pivot so weapon position circles around the character
		if (_gunPivot != null) _gunPivot.Scale = new Vector2(isFlipped ? -1.0f : 1.0f, 1.0f);
	}
}
