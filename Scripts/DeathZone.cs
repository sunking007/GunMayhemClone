using Godot;
using System;

namespace GunMayhemClone;

public partial class DeathZone : Area2D
{
	[Export] public float RespawnDelaySeconds = 1.5f;

	public override void _Ready()
	{
		this.BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		// Make sure it only triggers on active players who aren't already dead
		if (body is Player player && player.Visible)
		{
			HandlePlayerDeath(player);
		}
	}

	private async void HandlePlayerDeath(Player player)
	{
		if (player.IsInGroup("players"))
		{
			player.RemoveFromGroup("players");
		}

		// Hide character visual assets immediately
		player.Visible = false;

		// 1. Subtract exactly one life and instantly strip physics collisions
		bool isStillAlive = player.ProcessFallingLifePenalty();

		if (isStillAlive)
		{
			// 2. Wait out the respawn timer delay safely while the player node rests at their spawn marker
			await ToSignal(GetTree().CreateTimer(RespawnDelaySeconds), SceneTreeTimer.SignalName.Timeout);

			// 3. Bring them back visually
			player.Visible = true;
			player.AddToGroup("players");

			// 🛠️ RE-ENABLE MECHANICS: Re-engage the character's collision box matrix on the server
			player.CompleteRespawn();
		}
	}
}
