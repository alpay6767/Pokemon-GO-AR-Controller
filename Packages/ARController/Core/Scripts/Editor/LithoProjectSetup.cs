/////////////////////////////////////////////////
// LITHO SDK                                   //
// Copyright © 2020 Purple Tambourine Ltd.     //
// License: see LICENSE in package root folder //
/////////////////////////////////////////////////

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.Callbacks;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

namespace LITHO.UnityEditor
{

    /// <summary>
    /// Provides menu options to automate setting up a project for use with Litho
    /// </summary>
    public static class LithoProjectSetup
    {
        private static AddRequest _packageAddRequest;

        private static bool IsBusy
        {
            get
            {
                return _isBusy || _isListeningForUpdates;
            }
        }
        private static bool _isBusy;
        private static bool _isListeningForUpdates, _hideUnnecessaryAttempt;

        private const string CAMERA_USAGE_DESCRIPTION = "Used for augmented reality";
        private const string LOCATION_USAGE_DESCRIPTION
            = "Required for Bluetooth connection to Litho";
        private const string BLUETOOTH_USAGE_DESCRIPTION = "Required for connection to Litho";

        private const string ARKIT_PACKAGE = "com.unity.xr.arkit@3.1.0-preview.1";
        private const string ARCORE_PACKAGE = "com.unity.xr.arcore@3.1.0-preview.1";

        private const string MIN_IOS_SDK_VERSION = "12.2";
        private const AndroidSdkVersions MIN_ANDROID_SDK_VERSION
            = AndroidSdkVersions.AndroidApiLevel26;


        [MenuItem("LITHO/Project Setup/Configure For iOS", false, 1001)]
        private static void ConfigureForiOS()
        {
            _isBusy = true;
            int changeCount = 0;
            changeCount += UpdatePlayerSettingsiOS();

            if (changeCount > 0)
            {
                if (changeCount == 1)
                {
                    Debug.Log("Project settings were updated; 1 change was made");
                }
                else
                {
                    Debug.LogFormat("Project settings were updated; {0} changes were made",
                                    changeCount);
                }
            }
            else
            {
                Debug.Log("Project settings (iOS) are already up to date");
            }

            _isBusy = false;

            UpdateBuildPlatform(true);
            Debug.LogFormat("Attempting to install {0}", ARKIT_PACKAGE);
            UpdatePackage(ARKIT_PACKAGE);
        }

        [MenuItem("LITHO/Project Setup/Configure For iOS", true, 1001)]
        private static bool ConfigureForiOSValidate()
        {
            return !IsBusy;
        }

        [MenuItem("LITHO/Project Setup/Configure For Android", false, 1002)]
        private static void ConfigureForAndroid()
        {
            _isBusy = true;
            int changeCount = 0;
            changeCount += UpdatePlayerSettingsAndroid();

            if (changeCount > 0)
            {
                if (changeCount == 1)
                {
                    Debug.Log("Project settings were updated; 1 change was made");
                }
                else
                {
                    Debug.LogFormat("Project settings were updated; {0} changes were made",
                                    changeCount);
                }
            }
            else
            {
                Debug.Log("Project settings (iOS) are already up to date");
            }

            _isBusy = false;

            UpdateBuildPlatform(false);
            Debug.LogFormat("Attempting to install {0}", ARCORE_PACKAGE);
            UpdatePackage(ARCORE_PACKAGE);
        }

        [MenuItem("LITHO/Project Setup/Configure For Android", true, 1002)]
        private static bool ConfigureForAndroidValidate()
        {
            return !IsBusy;
        }

        // Returns the number of settings that were changed
        private static int UpdatePlayerSettingsiOS()
        {
            int changeCount = 0;
            // Update relevant player settings and notify the user via the Unity Console of any
            // changes that were made
            string oldString;

            // Fill in camera and location usage descriptions for iOS
            if (PlayerSettings.iOS.cameraUsageDescription != CAMERA_USAGE_DESCRIPTION)
            {
                oldString = PlayerSettings.iOS.cameraUsageDescription;
                PlayerSettings.iOS.cameraUsageDescription = CAMERA_USAGE_DESCRIPTION;
                changeCount++;
                Debug.LogFormat("iOS: Camera Usage Description was changed from '{0}' to '{1}'",
                                oldString, PlayerSettings.iOS.cameraUsageDescription);
            }
            if (PlayerSettings.iOS.locationUsageDescription != LOCATION_USAGE_DESCRIPTION)
            {
                oldString = PlayerSettings.iOS.locationUsageDescription;
                PlayerSettings.iOS.locationUsageDescription = LOCATION_USAGE_DESCRIPTION;
                changeCount++;
                Debug.LogFormat("iOS: Location Usage Description was changed from '{0}' to '{1}'",
                                oldString, PlayerSettings.iOS.locationUsageDescription);
            }

            // Set minimum iOS version
            if (PlayerSettings.iOS.targetOSVersionString != MIN_IOS_SDK_VERSION)
            {
                oldString = PlayerSettings.iOS.targetOSVersionString;
                PlayerSettings.iOS.targetOSVersionString = MIN_IOS_SDK_VERSION;
                changeCount++;
                Debug.LogFormat("iOS: Target OS Version was changed from '{0}' to '{1}'",
                                oldString, PlayerSettings.iOS.targetOSVersionString);
            }

            // Set fullscreen requirements
            if (!PlayerSettings.iOS.requiresFullScreen)
            {
                PlayerSettings.iOS.requiresFullScreen = true;
                changeCount++;
                Debug.Log("iOS: Fullscreen is now required");
            }

            // Disable orientation changes (Litho only supports portrait mode)
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                changeCount++;
                Debug.Log("iOS: Screen orientation is now constrained to portrait mode");
            }

            // Use IL2CPP as the scripting backend
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.iOS) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
                changeCount++;
                Debug.Log("iOS: Scripting backend changed to IL2CPP");
            }

            return changeCount;
        }

        // Returns the number of settings that were changed
        private static int UpdatePlayerSettingsAndroid()
        {
            int changeCount = 0;
            // Update relevant player settings and notify the user via the Unity Console of any
            // changes that were made
            string oldString;

            // Specifically DO NOT require ARCore support (this is handled by the ARCore package)
            if (PlayerSettings.Android.ARCoreEnabled)
            {
                PlayerSettings.Android.ARCoreEnabled = false;
                changeCount++;
                Debug.Log("Android: Disable ARCore support (this is handled by the ARCore " +
                          "package)");
            }

            // Set minimum Android operating system version
            if (PlayerSettings.Android.minSdkVersion != MIN_ANDROID_SDK_VERSION)
            {
                oldString = PlayerSettings.Android.minSdkVersion.ToString();
                PlayerSettings.Android.minSdkVersion = MIN_ANDROID_SDK_VERSION;
                changeCount++;
                Debug.LogFormat("Android: Minimum SDK Version was changed from '{0}' to '{1}'",
                                oldString, PlayerSettings.Android.minSdkVersion);
            }

            // Set fullscreen requirements
            if (!PlayerSettings.Android.startInFullscreen)
            {
                PlayerSettings.Android.startInFullscreen = true;
                changeCount++;
                Debug.Log("Android: Now starts in fullscreen");
            }

            // Disable orientation changes (Litho only supports portrait mode)
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
                changeCount++;
                Debug.Log("Android: Screen orientation is now constrained to portrait mode");
            }

            // Use IL2CPP as the scripting backend
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                changeCount++;
                Debug.Log("Android: Scripting backend changed to IL2CPP");
            }

            //Support 64-bit builds only
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                changeCount++;
                Debug.Log("Android: Player architecture set to ARM64");

            }

            return changeCount;
        }

        private static void UpdateBuildPlatform(bool iOS)
        {
            BuildTarget oldBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (iOS && oldBuildTarget != BuildTarget.iOS)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.iOS, BuildTarget.iOS);
            }
            else if (!iOS && oldBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android, BuildTarget.Android);
            }

            Debug.LogFormat("Switched build platform from {0} to {1}",
                            oldBuildTarget, EditorUserBuildSettings.activeBuildTarget);

            foreach (string str in EditorBuildSettings.GetConfigObjectNames())
            {
                Debug.Log(str);
            }
        }

        private static void UpdatePackage(string packageName, bool hideUnnecessaryAttempt = false)
        {
            EditorApplication.update += HandlePackageEvents;
            _isListeningForUpdates = true;
            _hideUnnecessaryAttempt = hideUnnecessaryAttempt;
            _packageAddRequest = Client.Add(packageName);
        }

        private static void HandlePackageEvents()
        {
            if (_packageAddRequest.IsCompleted)
            {
                if (_packageAddRequest.Status == StatusCode.Success)
                {
                    // Determine the state of the search request
                    switch (_packageAddRequest.Result.status)
                    {
                        case PackageStatus.Available:
                            if (!_hideUnnecessaryAttempt)
                            {
                                Debug.LogFormat("{0} is installed",
                                                _packageAddRequest.Result.packageId);
                            }
                            break;
                        case PackageStatus.Unavailable:
                            Debug.LogFormat("Could not install package {0}",
                                            _packageAddRequest.Result.packageId);
                            break;
                        case PackageStatus.Error:
                            Debug.LogErrorFormat(
                                "{0} installation failed with the following errors",
                                _packageAddRequest.Result.packageId);
                            foreach (Error error in _packageAddRequest.Result.errors)
                            {
                                Debug.LogError(error);
                            }
                            break;
                        case PackageStatus.Unknown:
                            Debug.LogWarningFormat("{0} is in an unknown state",
                                                   _packageAddRequest.Result.packageId);
                            break;
                        case PackageStatus.InProgress:
                            Debug.LogWarningFormat("{0} installation is in progress",
                                                   _packageAddRequest.Result.packageId);
                            break;
                    }
                }
                else
                {
                    Debug.LogError(_packageAddRequest.Error.message);
                }

                EditorApplication.update -= HandlePackageEvents;
                _isListeningForUpdates = false;
            }
        }


        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {

            if (buildTarget == BuildTarget.iOS)
            {
#if UNITY_IOS
                // Modify project properties as required
                string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
                PBXProject proj = new PBXProject();
                proj.ReadFromFile(projPath);

                // Disable bitcode - the Litho plugin is currently not compatible.

#if UNITY_2019_3_OR_NEWER
                string target = proj.GetUnityMainTargetGuid();
#else
                string target = proj.TargetGuidByName("Unity-iPhone");
#endif

                proj.SetBuildProperty(target, "ENABLE_BITCODE", "false");

                // Modify Info.plist as required
                string plistPath = path + "/Info.plist";
                PlistDocument info = new PlistDocument();
                info.ReadFromFile(plistPath);

                // The app will crash on load unless NSBluetoothAlwaysUsageDescription is set
                info.root.SetString("NSBluetoothAlwaysUsageDescription", "Required for connection to Litho");

                // Save the modified files
                proj.WriteToFile(projPath);
                info.WriteToFile(plistPath);
#endif
            }


        }
    }

}
#endif
