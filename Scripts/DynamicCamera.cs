using Godot;
using System;
using System.Collections.Generic;

namespace GunMayhemClone;

public partial class DynamicCamera : Camera2D
{
	[ExportGroup("Movement & Zoom")]
	[Export] public float SmoothSpeed = 6.0f;     
	[Export] public float MinZoom = 0.35f;         
	[Export] public float MaxZoom = 0.8f;          
	[Export] public float ZoomMargin = 300.0f;     

	[ExportGroup("Nested Parallax Tweak")]
	[Export] public float DistanceBgFactor = 0.35f; // Scale factor (0.35 means background shifts at 35% speed)

	private List<Node2D> _activeTargets = new List<Node2D>();
	private Vector2 _screenSize;
	private Node2D _distanceBgLayer;

	public override void _Ready()
	{
		_screenSize = GetViewportRect().Size;
		
		// Find your background node nested straight under the camera tree structure
		_distanceBgLayer = GetNodeOrNull<Node2D>("EnvironmentBackdrop/DistanceBGLayer");
		
		// Fully clear away boundaries so tracking operates smoothly
		this.LimitLeft = -1000000;
		this.LimitRight = 1000000;
		this.LimitTop = -1000000;
		this.LimitBottom = 1000000;
	}

	public override void _PhysicsProcess(double delta)
	{
		FindActivePlayers();

		if (_activeTargets.Count == 0) return;

		// 1. Calculate target center point position vectors
		Vector2 targetCenter = GetCenterPoint();
		Vector2 currentPos = GlobalPosition;
		Vector2 nextPos = currentPos.Lerp(targetCenter, (float)delta * SmoothSpeed);
		
		GlobalPosition = nextPos;

		// 2. Smoothly calculate and track dynamic camera lens zoom size
		float idealZoom = GetRequiredZoom();
		Vector2 targetZoom = new Vector2(idealZoom, idealZoom);
		Zoom = Zoom.Lerp(targetZoom, (float)delta * SmoothSpeed);

		// 3. Process Child-Relative Parallax Counter Displacement
		ApplyNestedParallax();
	}

	private void ApplyNestedParallax()
	{
		if (_distanceBgLayer == null) return;

		// Because the node is a child of the camera, it moves at 100% camera speed.
		// We subtract a fraction of the camera's local displacement vector to make it glide.
		Vector2 cameraTrackingShift = GlobalPosition;
		_distanceBgLayer.Position = -cameraTrackingShift * DistanceBgFactor;
	}

	private void FindActivePlayers()
	{
		_activeTargets.Clear();
		var playerNodes = GetTree().GetNodesInGroup("players");
		foreach (Node node in playerNodes)
		{
			if (node is Node2D node2D && GodotObject.IsInstanceValid(node2D))
			{
				_activeTargets.Add(node2D);
			}
		}
	}

	private Vector2 GetCenterPoint()
	{
		if (_activeTargets.Count == 1) return _activeTargets[0].GlobalPosition;

		var bounds = new Rect2(_activeTargets[0].GlobalPosition, Vector2.Zero);
		foreach (var target in _activeTargets)
		{
			bounds = bounds.Expand(target.GlobalPosition);
		}
		return bounds.GetCenter();
	}

	private float GetRequiredZoom()
	{
		if (_activeTargets.Count <= 1) return MaxZoom;

		var bounds = new Rect2(_activeTargets[0].GlobalPosition, Vector2.Zero);
		foreach (var target in _activeTargets)
		{
			bounds = bounds.Expand(target.GlobalPosition);
		}

		float widthWithMargin = bounds.Size.X + ZoomMargin;
		float heightWithMargin = bounds.Size.Y + ZoomMargin;

		float zoomX = _screenSize.X / widthWithMargin;
		float zoomY = _screenSize.Y / heightWithMargin;

		float optimalZoom = Mathf.Min(zoomX, zoomY);
		return Mathf.Clamp(optimalZoom, MinZoom, MaxZoom);
	}
}
