using Godot;
using System;
using System.Collections.Generic;

namespace GunMayhemClone;

public partial class DynamicCamera : Camera2D
{
	[ExportGroup("Movement & Zoom")]
	[Export] public float SmoothSpeed = 6.0f;     
	[Export] public float MinZoom = 0.35f;         // Zoomed out wide for distant players
	[Export] public float MaxZoom = 0.8f;          // Zoomed in closer for 1 player or tight fights
	[Export] public float ZoomMargin = 300.0f;     // Padding around players so they don't hit the screen edge

	private List<Node2D> _activeTargets = new List<Node2D>();
	private Vector2 _screenSize;

	public override void _Ready()
	{
		_screenSize = GetViewportRect().Size;
	}

	public override void _PhysicsProcess(double delta)
	{
		// 1. Find all alive players in the scene dynamically
		FindActivePlayers();

		// If no players are alive (everyone dead/respawning), do not move the camera
		if (_activeTargets.Count == 0) return;

		// 2. Smoothly transition position to the exact midpoint center of all alive players
		Vector2 targetCenter = GetCenterPoint();
		Position = Position.Lerp(targetCenter, (float)delta * SmoothSpeed);

		// 3. Smoothly calculate and track dynamic camera lens zoom size
		float idealZoom = GetRequiredZoom();
		Vector2 targetZoom = new Vector2(idealZoom, idealZoom);
		Zoom = Zoom.Lerp(targetZoom, (float)delta * SmoothSpeed);
	}

	/// <summary>
	/// Finds any node currently in the scene that belongs to the "players" group.
	/// This handles solo play, 2-player local matches, and automatic removal during respawns.
	/// </summary>
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

		// Create a bounding box enclosing all active player coordinates
		var bounds = new Rect2(_activeTargets[0].GlobalPosition, Vector2.Zero);
		foreach (var target in _activeTargets)
		{
			bounds = bounds.Expand(target.GlobalPosition);
		}
		return bounds.GetCenter();
	}

	private float GetRequiredZoom()
	{
		// If only 1 player is alive/left on screen, lock it to the closer MaxZoom setting
		if (_activeTargets.Count <= 1) return MaxZoom;

		// Calculate the bounding box of the distance between players
		var bounds = new Rect2(_activeTargets[0].GlobalPosition, Vector2.Zero);
		foreach (var target in _activeTargets)
		{
			bounds = bounds.Expand(target.GlobalPosition);
		}

		// Calculate needed zoom based on player distances + margin buffer
		float widthWithMargin = bounds.Size.X + ZoomMargin;
		float heightWithMargin = bounds.Size.Y + ZoomMargin;

		float zoomX = _screenSize.X / widthWithMargin;
		float zoomY = _screenSize.Y / heightWithMargin;

		// Use the smaller zoom factor to guarantee all players stay perfectly framed on screen
		float optimalZoom = Mathf.Min(zoomX, zoomY);
		return Mathf.Clamp(optimalZoom, MinZoom, MaxZoom);
	}
}
