using System;
using System.Collections.Generic;
using Godot;
using godot_getnode;
using SharpHook;
using MB = SharpHook.Native.MouseButton;

// TODO: test on windows
// TODO: slowly fade bullet hole away
public partial class Main : Node3D
{
	const int NUM_SUB_WINDOWS = 10;
	const float Z_ROT_RANGE = 10;
	const float X_ROT_RANGE = 30;
	const float X_MID_ANGLE = 90;

	Vector3 basePosition = Vector3.Zero;
	PackedScene bulletHoleWindowScene = GD.Load<PackedScene>("res://bullet_hole_window.tscn");
	StringName currentSpinAnim = ActionNameNone;
	BulletHoleWindow[] subWindows = new BulletHoleWindow[NUM_SUB_WINDOWS];


	[GetNode(Unique: true)] Node3D Pistol;
	[GetNode(Unique: true)] Node3D PistolPivot;
	[GetNode(Unique: true)] AnimationPlayer AnimationPlayer;
	[GetNode(Unique: true)] Timer Timer;
	[GetNode(Unique: true)] TextureRect Preview;
	[GetNode(Unique: true)] OmniLight3D OmniLight3D;
	[GetNode(Unique: true)] AudioStreamPlayer PistolAudio;

	public override void _Ready()
	{
		this.GetAnnotatedNodes();

		for (var i = 0; i < NUM_SUB_WINDOWS; i++)
		{
			subWindows[i] = bulletHoleWindowScene.Instantiate<BulletHoleWindow>();
			AddChild(subWindows[i]);
		}


		// TODO: When I set "No Focus" to true on project settings,
		// Borderless becomes true and immutable
		// and the window automatically gets displayed on all desktops.
		// Is KDE doing that?

		var win = GetWindow();
		win.Transparent = true;
		win.TransparentBg = true;
		win.MousePassthrough = true;
		//win.Unfocusable = true;
		win.Borderless = true;
		//win.AlwaysOnTop = true;

		OnTimeOut();
		Timer.Timeout += OnTimeOut;

		var hook = new SimpleGlobalHook(GlobalHookType.Mouse);
		hook.MousePressed += OnMouseClicked;
		hook.MouseWheel += OnMouseWheel;
		hook.RunAsync();

		Callable.From(() =>
		{
			var screen_size = DisplayServer.ScreenGetSize();
			win.Position = new Vector2I(screen_size.X - win.Size.X, screen_size.Y - win.Size.Y);
		}).CallDeferred();

		basePosition = PistolPivot.Position;
	}

	private void OnMouseWheel(object sender, MouseWheelHookEventArgs e)
	{
		var rotation = e.Data.Rotation;
		var animation = rotation < 0 ? ActionNameSpinDown : ActionNameSpinUp;
		if (!(animation == currentSpinAnim && AnimationPlayer.IsPlaying()))
		{
			currentSpinAnim = animation;
			Callable.From(() => AnimationPlayer.Play(animation)).CallDeferred();
		}
	}


	private void OnTimeOut()
	{
		var img = DisplayServer.ScreenGetImage(DisplayServer.GetPrimaryScreen());
		img.Resize(20, 20, Image.Interpolation.Nearest);
		Preview.Texture.Dispose();
		Preview.Texture = ImageTexture.CreateFromImage(img);
		var size = img.GetSize();
		var color = img.GetPixel(0, 0);
		for (var x = 0; x < size.X; x++)
		{
			for (var y = 0; y < size.Y; y++)
			{
				var c = img.GetPixel(x, y);
				color = color.Lerp(c, c.Luminance > color.Luminance ? 0.5f : 0.05f);
			}
		}

		OmniLight3D.LightColor = color;
	}

	private void OnMouseClicked(object sender, MouseHookEventArgs e)
	{
		var btn = e.Data.Button;
		if (btn == MB.Button1 || btn == MB.Button2)
		{
			Callable.From(() =>
			{
				Fire(btn == MB.Button1 ? ActionNameFire : ActionNameFire2, new Vector2I(e.Data.X, e.Data.Y));
			}).CallDeferred();
		}
		else if (btn == MB.Button3)
		{
			Callable.From(() =>
			{
				var animation = GD.Randf() < 0.3 ? ActionNameFire2 : ActionNameFire;
				BurstFire(animation, new Vector2I(e.Data.X, e.Data.Y));
			}).CallDeferred();
		}
	}

	public override void _Process(double delta)
	{
		var mouse_pos = DisplayServer.MouseGetPosition();
		var screen_size = DisplayServer.ScreenGetSize();

		var mx = mouse_pos.X;
		var my = mouse_pos.Y;
		var x = (mx / ((float)screen_size.X / 2) - 1) * -Z_ROT_RANGE;
		var y = X_MID_ANGLE - (mx / ((float)screen_size.X / 2) - 1) * X_ROT_RANGE;
		var z = (my / ((float)screen_size.Y / 2) - 1) * -Z_ROT_RANGE;

		PistolPivot.RotationDegrees = PistolPivot.RotationDegrees with { X = -x, Y = y, Z = z };
		PistolPivot.Position = basePosition + new Vector3
		{
			Y = (my / ((float)screen_size.Y / 2) - 1) * -0.10f,
		};
	}

	void Fire(StringName animation, Vector2I mousePos)
	{
		PistolAudio.Playing = true;
		AnimationPlayer.Stop();
		AnimationPlayer.Play(animation);

		var bulletWindow = subWindows[0];
		foreach (var win in subWindows)
		{
			if (win.LastUpdate < bulletWindow.LastUpdate)
			{
				bulletWindow = win;
			}
		}
		bulletWindow.Clear();
		bulletWindow.Shoot();
		bulletWindow.Position = mousePos - bulletWindow.Size / 2;
	}

	async void BurstFire(StringName animation, Vector2I mousePos)
	{
		for (var i = 0; i < 3; i++)
		{
			Fire(animation, mousePos);
			var timer = GetTree().CreateTimer(0.10);
			await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
		}
	}


	static StringName ActionNameNone = "-";
	static StringName ActionNameFire = "Fire";
	static StringName ActionNameFire2 = "Fire2";
	static StringName ActionNameSpinDown = "SpinDown";
	static StringName ActionNameSpinUp = "SpinUp";
}
