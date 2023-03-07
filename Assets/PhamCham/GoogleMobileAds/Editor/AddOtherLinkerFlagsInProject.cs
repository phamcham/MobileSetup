using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public class AddOtherLinkerFlagsInProject {
#if UNITY_IOS

    const string KEY_OTHER_LDFLAGS = "OTHER_LDFLAGS";
    const string VALUE_OBJC = "-ObjC";

    [PostProcessBuild(500)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject) {
        if (target != BuildTarget.iOS) {
            return;
        }

        string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
        PBXProject proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(projPath));

        string targetUnityMainGUID = proj.GetUnityMainTargetGuid();
        string projectGUID = proj.ProjectGuid();
        //string targetUnityFrameworkGUID = proj.GetUnityFrameworkTargetGuid();
        //string targetUnityTestGUID = PBXProject.GetUnityTestTargetName();

        proj.UpdateBuildProperty(targetUnityMainGUID, KEY_OTHER_LDFLAGS, new List<string>() { VALUE_OBJC }, new List<string>());
        proj.UpdateBuildProperty(projectGUID, KEY_OTHER_LDFLAGS, new List<string>() { VALUE_OBJC }, new List<string>());

        //proj.AddBuildProperty(targetUnityMainGUID, KEY_OTHER_LDFLAGS, "-ObjC");
        //proj.AddBuildProperty(targetUnityTestGUID, KEY_OTHER_LDFLAGS, VALUE_OBJC);

        File.WriteAllText(projPath, proj.WriteToString());

        Debug.Log("Set \"OTHER_LDFLAGS\" is \"-ObjC\" to Build Settings");
    }
#endif
}
