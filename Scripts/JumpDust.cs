using Godot;
using System;

namespace GunMayhemClone;

public partial class JumpDust : GpuParticles2D
{
	
	public override void _Ready()
	{
		Finished += () => QueueFree();
	}
}
