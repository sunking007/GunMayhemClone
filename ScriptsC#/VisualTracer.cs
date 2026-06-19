using Godot;
using System;

namespace GunMayhemClone;

public partial class VisualTracer : Line2D
{
	[Export] public float Speed = 2000.0f;
	[Export] public float MaxDistance = 1500.0f;

	public Vector2 Direction = Vector2.Right;
	private Vector2 _startPosition;

	public override void _Ready()
	{
		_startPosition = GlobalPosition;
		TopLevel = true; // Decoupled coordinate movement systems
	}

	public override void _PhysicsProcess(double delta)
	{
		// Move forward independently through world space
		GlobalPosition += Direction * Speed * (float)delta;

		// Clear object instantly once it finishes traveling the exact specified impact distance length
		if (GlobalPosition.DistanceTo(_startPosition) >= MaxDistance)
		{
			QueueFree();
		}
	}
}
