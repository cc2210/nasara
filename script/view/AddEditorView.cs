using Godot;
using Semver;
using System;
using Humanizer;

public partial class AddEditorView : Control
{
	GodotManager godotManager;
	GodotRequester godotRequester;

	Godot.Collections.Array<DownloadableVersion> stableVersions = new();
	Godot.Collections.Array<DownloadableVersion> unstableVersions = new();
	Godot.Collections.Array<GodotVersion> installedVersions = new();

	[ExportGroup("Pages")] // TODO: Refactor these to make code clean
	[Export]
	Control pageInstallType;
	[Export]
	Control pageInstallSetting;
	[Export]
	Control pageInstallDownloading;

	[ExportGroup("Install Type Page")]
	[Export]
	BaseButton installButton;
	[Export]
	BaseButton addExistsButton;
	[Export]
	CheckButton monoCheckButton;

	[ExportGroup("Install Setting Page")]
	[Export]
	OptionButton channelOption;
	[Export]
	OptionButton versionOption;
	[Export]
	BaseButton continueButton;
	[Export]
	Label alreadyInstalled;

	[ExportGroup("Install Downloading Page")]
	[Export]
	Label progressLabel;
	[Export]
	ProgressBar progressBar;
	[Export]
	BaseButton finishButton;


	[Signal]
	public delegate void AddedEditorEventHandler();


	// TODO: Add Cancel when Downloading

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SwitchView(0);
		installButton.Pressed += () => SwitchView(1);
		continueButton.Pressed += DownloadTargetVersion;
		finishButton.Pressed += () => EmitSignal(SignalName.AddedEditor);
		channelOption.ItemSelected += (long index) => {
			switch (index)
			{
				case (int)GodotVersion.VersionChannel.Stable:
					versionOption.Clear();
					foreach (DownloadableVersion version in stableVersions)
						versionOption.AddItem(version.Version.ToString());
					break;
				case (int)GodotVersion.VersionChannel.Unstable:
					versionOption.Clear();
					foreach (DownloadableVersion version in unstableVersions)
						versionOption.AddItem(version.Version.ToString());
					break;
			}
		};
		versionOption.ItemSelected += InstallVersionSelected;
		
		monoCheckButton.ButtonPressed = false;
		finishButton.Visible = false;
		alreadyInstalled.Visible = false;

		godotManager = GetNode<GodotManager>("/root/GodotManager");
		installedVersions = godotManager.GetVersions();
		godotRequester = godotManager.GetRequester();

		godotRequester.RequestCompleted += (Godot.Collections.Array<DownloadableVersion> downloadableVersions, int channel) =>
		{
			switch (channel)
			{
				case (int)GodotVersion.VersionChannel.Stable:
					stableVersions = downloadableVersions; break;
				case (int)GodotVersion.VersionChannel.Unstable:
					unstableVersions = downloadableVersions; break;
			}
			if (channelOption.Selected == channel)
			{
				versionOption.Clear();
				foreach (DownloadableVersion version in downloadableVersions)
					versionOption.AddItem(version.Version.ToString());
			}

			/* FIXME: Out Of Range
			E 0:00:13:0100   Array.cs:1007 @ void Godot.Collections.Array.GetVariantBorrowElementAt(int, Godot.NativeInterop.godot_variant&): System.ArgumentOutOfRangeException: Specified argument was out of the range of valid values. (Parameter 'index')
			<C# 错误>        System.ArgumentOutOfRangeException
			<C# 源文件>      /root/godot/modules/mono/glue/GodotSharp/GodotSharp/Core/Array.cs:1007 @ void Godot.Collections.Array.GetVariantBorrowElementAt(int, Godot.NativeInterop.godot_variant&)
			<栈追踪>         Array.cs:1007 @ void Godot.Collections.Array.GetVariantBorrowElementAt(int, Godot.NativeInterop.godot_variant&)
							Array.cs:1408 @ T Godot.Collections.Array`1.get_Item(int)
							AddEditorView.cs:109 @ void AddEditorView.InstallVersionSelected(long)
							AddEditorView.cs:94 @ void AddEditorView.<_Ready>b__17_2(Godot.Collections.Array`1[DownloadableVersion], int)
							GodotRequester_ScriptSignals.generated.cs:40 @ void GodotRequester.RaiseGodotClassSignalCallbacks(Godot.NativeInterop.godot_string_name&, Godot.NativeInterop.NativeVariantPtrArgs)
							ScriptManagerBridge.cs:383 @ void Godot.Bridge.ScriptManagerBridge.RaiseEventSignal(nint, Godot.NativeInterop.godot_string_name*, Godot.NativeInterop.godot_variant**, int, Godot.NativeInterop.godot_bool*)
			*/
			InstallVersionSelected(0);
		};

		godotRequester.RequestEditorList();
		godotRequester.RequestEditorList(GodotVersion.VersionChannel.Unstable);
	}

	void InstallVersionSelected(long index)
	{
		continueButton.Disabled = false;
		monoCheckButton.Disabled = false;
		alreadyInstalled.Visible = false;

		DownloadableVersion selectedVersion;
		if (channelOption.Selected == (int)GodotVersion.VersionChannel.Stable)
			selectedVersion = stableVersions[(int)index]; // Why not use the `Text`?
		else
			selectedVersion = unstableVersions[(int)index];
		
		bool hasMono = false;
		bool hasGds = false;

		foreach (GodotVersion version in installedVersions)
		{
			if (version.Version.Equals(selectedVersion.Version))
				if (version.Mono)
					hasMono = true;
				else
					hasGds = true;
				//GD.Print(version.Version.ToString(), version.Mono);
		}

		//GD.Print(selectedVersion.Version.ToString(), hasMono, hasGds);

		if (hasMono && hasGds)
		{
			continueButton.Disabled = true;
			monoCheckButton.Disabled = true;
			alreadyInstalled.Visible = true;
		} else if (hasMono)
		{
			monoCheckButton.ButtonPressed = false;
			monoCheckButton.Disabled = true;
		} else if (hasGds)
		{
			monoCheckButton.ButtonPressed = true;
			monoCheckButton.Disabled = true;
		}
	}

	void SwitchView(int index)
	{
		switch (index)
		{
			case 0:
				pageInstallType.Visible = true;
				pageInstallSetting.Visible = false;
				pageInstallDownloading.Visible = false;
				break;
			case 1:
				pageInstallType.Visible = false;
				pageInstallSetting.Visible = true;
				pageInstallDownloading.Visible = false;
				break;
			case 2:
				pageInstallType.Visible = false;
				pageInstallSetting.Visible = false;
				pageInstallDownloading.Visible = true;
				break;
		}
	}

	void DownloadTargetVersion()
	{
			if (channelOption.Selected == (int)GodotVersion.VersionChannel.Stable)
			{ 
				foreach (DownloadableVersion version in stableVersions)
				{
					if (version.Version.Equals(SemVersion.Parse(versionOption.Text, SemVersionStyles.Any)))
					{
						// Using Unstable to fix
						// TODO: Remove it
						if (version.GetDownloadUrl(GetTargetPlatform()) is not null)
						{
							StartDownload(version); // FIXME: Get a null url when requesting the latest stable version
							return;
						}
						else
						{
							GD.PushWarning("[BUG] Get a null url when requesting the latest stable version");
							GD.PushWarning("Failed to Get Download Link from godotengine/godot, Trying to use godotengine/godot-builds");
							
							foreach (DownloadableVersion i in unstableVersions)
								if (i.Version.Equals(SemVersion.Parse(versionOption.Text, SemVersionStyles.Any)))
								{
									if (i.GetDownloadUrl(GetTargetPlatform()) is not null)
									{
										StartDownload(i);
										return;
									}
								}
							
							GD.PushError("Failed to Get Download Link from godotengine/godot-builds again");
						}
						
					}
				}
			}
			else
			{
				foreach (DownloadableVersion _version in unstableVersions)
				{
					if (_version.Version.Equals(SemVersion.Parse(versionOption.Text, SemVersionStyles.Any)))
					{
						StartDownload(_version);
						return;
					}
				}
			}
			//StartDownload(unstableVersions[versionOption.Selected]);
		}

	DownloadableVersion.TargetPlatform GetTargetPlatform()
	{
		DownloadableVersion.TargetPlatform platform = DownloadableVersion.TargetPlatform.Win32;

		// FIXME: May got problems in build of x86 running on x86_64 pc
		// 64-bit
		if (OS.HasFeature("x86_64"))
			if (OS.HasFeature("windows"))
				if (monoCheckButton.ButtonPressed)
					platform = DownloadableVersion.TargetPlatform.Win64Mono;
				else
					platform = DownloadableVersion.TargetPlatform.Win64;
			else if (OS.HasFeature("linux")) // Not Supported Yet
				GD.PushError("Does Not Support Linux Yet");
		// 32-bit
		else if (OS.HasFeature("x86_32"))
			if (OS.HasFeature("windows"))
				if (monoCheckButton.ButtonPressed)
					platform = DownloadableVersion.TargetPlatform.Win32Mono;
				else
					platform = DownloadableVersion.TargetPlatform.Win32;
			else if (OS.HasFeature("linux")) // Not Supported Yet
				GD.PushError("Does Not Support Linux Yet");
		
		return platform;
	}

	void StartDownload(DownloadableVersion version)
	{
		string savePath = "user://godot_editor.dl";

		SwitchView(2);
        HttpRequest http = new()
        {
            DownloadFile = savePath,
			UseThreads = true
        };
        AddChild(http);

		string version_str = version.Version.ToString();
		if (monoCheckButton.ButtonPressed)
			version_str = "Mono " + version_str;

        Timer timer = new()
        {
            WaitTime = 0.1
        };
        AddChild(timer);

		timer.Timeout += () => {
			int bodySize = http.GetBodySize();
			int downloaded = http.GetDownloadedBytes();
			if (bodySize != -1)
			{
				progressLabel.Text = "Downloading Godot " + version_str 
									+ $" ({downloaded.Bytes().Humanize()}/{bodySize.Bytes().Humanize()})";
				if (progressBar.MaxValue != bodySize) // do i actually need this
					progressBar.MaxValue = bodySize;
				progressBar.Value = downloaded;
			}

		};

		progressLabel.Text = "Requesting Godot " + version_str;

		// Get Url
		string url = version.GetDownloadUrl(GetTargetPlatform());

		// http.Request(url);
		http.Request("https://mirror.ghproxy.com/"+url); // TODO: Replace it
		GD.Print("GET ", url);

		http.RequestCompleted += (long result, long responseCode, string[] headers, byte[] body) => {
			if (result == (long)HttpRequest.Result.Success)
			{
				GD.Print(responseCode, " OK");
				progressBar.Value = progressBar.MaxValue + 1;
				progressLabel.Text = "Download Completed";

				// Rename File
				// DirAccess.Rename();
				/*
				foreach (string line in headers)
					GD.Print(line);
				*/
				UnpackGodot(version, savePath);
			}
			http.QueueFree();
			timer.QueueFree();
		};
		timer.Start();
	}

	void VerifyFile()
	{
		// TODO: Verifying Files using Sha-512
		// Use System.Security.Cryptography.SHA512
	}

	void UnpackGodot(DownloadableVersion version, string zipPath)
	{
		progressBar.Value = 0;
		progressLabel.Text = "Unzipping Files";

		// Unzipping Godot
		ZipReader zip = new();
		if (zip.Open(zipPath) != Error.Ok)
		{
			GD.PushError("Failed to Read Zip: ", zipPath);
			return;
		}

		string[] totalFiles = zip.GetFiles();
		progressBar.MaxValue = totalFiles.Length;

		string path = "user://editors";
		// Special Processing when not mono
		if (!monoCheckButton.ButtonPressed)
			path = path.PathJoin(totalFiles[0].GetBaseName());

		if (!DirAccess.DirExistsAbsolute(path))
			DirAccess.MakeDirRecursiveAbsolute(path);

		foreach (string filePath in totalFiles)
		{
			GD.Print("Unzipping  ", filePath);
			byte[] content = zip.ReadFile(filePath);
			string absFilePath = path.PathJoin(filePath);

			using var file = FileAccess.Open(absFilePath, FileAccess.ModeFlags.Write);

			// if not a file, then it's a dir
			if (file is null)
			{
				if (!DirAccess.DirExistsAbsolute(absFilePath))
				{
					GD.Print("Creating   ", filePath);
					DirAccess.MakeDirRecursiveAbsolute(absFilePath);
					progressBar.Value += 1;
					continue;
				}
				/*
				GD.PushError("Failed to Unzip: ", FileAccess.GetOpenError());
				return;
				*/
			}
			
			file.StoreBuffer(content);
			progressBar.Value += 1;
		}

		zip.Close();
		
		// Setting up
		progressLabel.Text = "Completed";

		string editorPath = path.PathJoin(totalFiles[0]);
		if (!monoCheckButton.ButtonPressed)
			editorPath = path;

		GD.Print($"Unzipped to {editorPath}");

		FinishInstalling(version, zipPath, editorPath);
	}

	void FinishInstalling(DownloadableVersion version, string zipPath, string editorPath)
	{
		// godotManager.AddVersion(new GodotVersion(version.Version, editorPath, version.Channel, monoCheckButton.ButtonPressed));
		// Because of bug above, we use this:
		GodotVersion.VersionChannel versionChannel = GodotVersion.VersionChannel.Stable;
		if (version.Version.IsPrerelease)
			versionChannel = GodotVersion.VersionChannel.Unstable;
		godotManager.AddVersion(new GodotVersion(version.Version, editorPath, versionChannel, monoCheckButton.ButtonPressed));

		// Cleaning up
		DirAccess.RemoveAbsolute(zipPath);
		if (monoCheckButton.ButtonPressed)
			GD.Print("Added Godot Mono ", version.Version.ToString(), "; Prerelease=", version.Version.IsPrerelease);
		else
			GD.Print("Added Godot ", version.Version.ToString(), "; Prerelease=", version.Version.IsPrerelease);
		
		finishButton.Visible = true;
	}
}
