// Copyright (c) Ryan Schmidt - rms@gradientspace.com - twitter @rms80
// released under the MIT License (see LICENSE file)
#if UNITY_EDITOR_OSX || UNITY_IOS

using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using UnityEditor.iOS.Xcode;
using System.IO;


public class XCodePListEdits
{

	/// <summary>
	/// This script updates the Info.plist file for XCode. You can
	/// do multiple things in here:
	///   1) change the iOS version number in code (commented out right now)
	///   2) Turn on other iOS settings (eg UIFileSharingEnabled, to enable iTunes sharing)
	/// </summary>
	[PostProcessBuild]
	public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
	{

		if (buildTarget == BuildTarget.iOS) {

			// Get plist
			string plistPath = pathToBuiltProject + "/Info.plist";
			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));

			// Get root
			PlistElementDict rootDict = plist.root;

			// turn on iTunes file sharing, so we can get files from device in iTunes
			rootDict.SetBoolean("UIFileSharingEnabled", true);

			// Change value of CFBundleVersion in Xcode plist
			//var buildKey = "CFBundleVersion";
			//rootDict.SetString(buildKey, "2.3.4");

			// Write to file
			File.WriteAllText(plistPath, plist.WriteToString());
		}
	}
}

#endif