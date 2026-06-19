using Godot;
using System;

namespace GunMayhemClone;

public partial class DeathZone : Area2D
{
	// The height where players will reappear when they respawn
	[Export] public float RespawnYPosition = 100.0f;
	[Export] public float RespawnDelaySeconds = 1.5f;

	public override void _Ready()
	{
		// Connect the Godot signal that triggers when any body enters this area
		this.BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		// Check if the falling object is a Player
		if (body is Player player)
		{
			HandlePlayerDeath(player);
		}
	}

	private async void HandlePlayerDeath(Player player)
	{
		// 1. Temporarily remove the player from the "players" group.
		// This causes the DynamicCamera to instantly drop them as a target and zoom into the surviving player!
		if (player.IsInGroup("players"))
		{
			player.RemoveFromGroup("players");
		}

		// 2. Hide the player and turn off their physics processing so they are "dead"
		player.Visible = false;
		player.ProcessMode = ProcessModeEnum.Disabled;

		// 3. Wait out the respawn timer delay
		await ToSignal(GetTree().CreateTimer(RespawnDelaySeconds), SceneTreeTimer.SignalName.Timeout);

		// 4. Reset player position back to the top-center area of the screen
		player.GlobalPosition = new Vector2(500.0f, RespawnYPosition);
		player.Velocity = Vector2.Zero;

		// 5. Re-enable the player and bring them back to life
		player.Visible = true;
		player.ProcessMode = ProcessModeEnum.Inherit;

		// 6. Put them back in the camera group so the camera smoothly pans and zooms out to capture them again
		player.AddToGroup("players");
	}
}
