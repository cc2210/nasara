using Godot;
using System;

public partial class AppInfoPopup : Window
{

	[Export]
	Label versionLabel;

	[Export]
	Label versionDetail;

	[Export]
	RichTextLabel richTextLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CloseRequested += Hide;

		richTextLabel.MetaClicked += OpenLink;

		versionLabel.Text = "Nasara v" + (string)ProjectSettings.GetSetting("application/config/version");

		versionDetail.Text =
		"Nasara v" + (string)ProjectSettings.GetSetting("application/config/version") + " (" +
		Engine.GetArchitectureName() + ")" + "\n" +
		"Data dir: " + OS.GetUserDataDir() + "\n" +
		"Lang: " + OS.GetLocaleLanguage();

		Godot.Collections.Dictionary engineInfo = Engine.GetVersionInfo();
		versionDetail.Text += $"\nBuild on Godot {engineInfo["string"]}";

		if (OS.IsDebugBuild())
			versionDetail.Text += "\nDebug Build";
	}

    private void OpenLink(Variant meta)
    {
        string link = (string)meta;
		OS.ShellOpen(link);
    }
}
