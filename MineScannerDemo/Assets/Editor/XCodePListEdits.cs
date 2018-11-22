using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using UnityEditor.iOS.Xcode;
using System.IO;

public class ChangeIOSBuildNumber
{

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