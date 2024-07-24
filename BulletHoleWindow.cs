using Godot;
using godot_getnode;
using System;

public partial class BulletHoleWindow : Window
{
	[GetNode(Unique: true)] BulletHoleCanvas Canvas;

	public long LastUpdate { get; set; }

	public override void _Ready()
	{
		this.GetAnnotatedNodes();
	}

	public bool ContainsPoint(Vector2 pos)
	{
		return pos.X >= Position.X && pos.X < Position.X + Size.X &&
		       pos.Y >= Position.Y && pos.Y < Position.Y + Size.Y;
	}

	public void Shoot()
	{
		Canvas.Shoot();
		LastUpdate = GetTree().GetFrame();
	}

	public void Clear()
	{
		Canvas.Clear();
	}
}
