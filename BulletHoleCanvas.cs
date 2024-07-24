using System;
using System.Collections.Generic;
using Godot;

public partial class BulletHoleCanvas : Node2D
{
	List<Hole> holes = new();
	static Texture2D bulletHole1 = GD.Load<Texture2D>("res://Assets/bullet-hole1.png");
	static Texture2D bulletHole2 = GD.Load<Texture2D>("res://Assets/bullet-hole2.png");
	static Vector2 baseHoleSize = new(50, 50);
	static Color[] colors = [
				Colors.White,
				Colors.LightBlue,
				Colors.LightGray,
				Colors.LightCyan,
			];

	public void Shoot()
	{
		holes.Add(new Hole
		{
			Offset = new Vector2(
				-0.1f * GD.Randf() * 0.1f,
				-0.1f * GD.Randf() * 0.1f
			),
			Color = colors[GD.Randi() % colors.Length],

			Scale = 0.6f + GD.Randf() * 0.4f,
			Rotation = GD.Randf() * (float)Math.PI,
			ImageType = GD.Randi() % 1,
		});
		QueueRedraw();
	}

	public override void _Draw()
	{
		foreach (var hole in holes)
		{
			DrawImage(hole);
		}
	}

	void DrawImage(Hole hole)
	{
		var winSize = GetWindow().Size;
		var center = new Vector2(winSize.X / 2, winSize.Y / 2);
		var texture = hole.ImageType == 0 ? bulletHole1 : bulletHole2;
		var size = baseHoleSize * hole.Scale;
		DrawSetTransform(center, hole.Rotation);
		DrawTextureRect(texture, new Rect2
		{
			Position = -size / 2,
			Size = size,
		}, false, hole.Color);
	}

	public void Clear()
	{
		holes.Clear();
	}

	class Hole
	{
		public Vector2 Offset { get; set; } = Vector2.Zero;
		public Color Color { get; set; } = Colors.White;
		public float Scale { get; set; } = 1;

		public float Rotation { get; set; }
		public uint ImageType { get; set; }
	}
}
