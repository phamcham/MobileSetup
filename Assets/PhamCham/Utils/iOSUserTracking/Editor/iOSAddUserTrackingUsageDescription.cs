using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

class iOSAddUserTrackingUsageDescription {
#if UNITY_IOS
    const string KEY_USERTRACKING = "NSUserTrackingUsageDescription";
    const string VALUE_USERTRACKING = "Your data will be used to provide you a better and personalized ad experience.";

    [PostProcessBuild(500)]
    public static void ChangeXcodePlist(BuildTarget buildTarget, string pathToBuiltProject) {
        if (buildTarget == BuildTarget.iOS) {
            // Get plist
            string plistPath = pathToBuiltProject + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            // Get root
            PlistElementDict rootDict = plist.root;

            // add user tracking description, neu them o localization roi thi thoi, cai nay de de phong apple mail thieu
            rootDict.SetString(KEY_USERTRACKING, VALUE_USERTRACKING);

            // Write to file
            File.WriteAllText(plistPath, plist.WriteToString());
        }
    }
#endif
}