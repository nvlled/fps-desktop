using System;
using Godot;
using godot_getnode;
using SharpHook;

// TODO: add burst fire mode (right-click)
// TODO: test on windows
// TODO: add damage effect
public partial class Main : Node3D
{
	const float Z_ROT_RANGE = 10;
	const float X_ROT_RANGE = 30;
	const float X_MID_ANGLE = 90;

	Vector3 basePosition = Vector3.Zero;

	[GetNode(Unique: true)] Node3D Pistol;
	[GetNode(Unique: true)] Node3D PistolPivot;
	//[GetNode(Unique: true)] GpuParticles3D FireParticles;
	[GetNode(Unique: true)] AnimationPlayer AnimationPlayer;
	[GetNode(Unique: true)] Timer Timer;
	[GetNode(Unique: true)] TextureRect Preview;
	[GetNode(Unique: true)] OmniLight3D OmniLight3D;
	[GetNode(Unique: true)] AudioStreamPlayer PistolAudio;

	public override void _Ready()
	{
		this.GetAnnotatedNodes();

		//FireLight.LightEnergy = 0;

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
		hook.RunAsync();

		Callable.From(() =>
		{
			var screen_size = DisplayServer.ScreenGetSize();
			win.Position = new Vector2I(screen_size.X - win.Size.X, screen_size.Y - win.Size.Y);
		}).CallDeferred();

		basePosition = PistolPivot.Position;

		GetWindow().SizeChanged += WindowSizeChanged;
	}

	private void WindowSizeChanged()
	{
		GD.Print(GetWindow().Size);
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
		if (e.Data.Button == SharpHook.Native.MouseButton.Button1)
		{
			Callable.From(() =>
			{
				PistolAudio.Playing = true;
				AnimationPlayer.Stop();
				AnimationPlayer.Play(ActionNameFire);
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


	static StringName ActionNameFire = "Fire";
}
