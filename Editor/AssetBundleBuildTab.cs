using AssetBundleBrowser.AssetBundleDataSource;

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEditor;

using UnityEngine;

namespace AssetBundleBrowser
{
    [System.Serializable]
    internal class AssetBundleBuildTab
    {
        private readonly string m_streamingPath = "Assets/StreamingAssets";

        [SerializeField]
        private Vector2 m_ScrollPosition;

        private class ToggleData
        {
            internal ToggleData(bool s,
                string title,
                string tooltip,
                List<string> onToggles,
                BuildAssetBundleOptions opt = BuildAssetBundleOptions.None)
            {
                state = onToggles.Contains(title) || s;
                content = new GUIContent(title, tooltip);
                option = opt;
            }
            //internal string prefsKey
            //{ get { return k_BuildPrefPrefix + content.text; } }
            internal bool state;
            internal GUIContent content;
            internal BuildAssetBundleOptions option;
        }

        private AssetBundleInspectTab m_InspectTab;

        [SerializeField]
        private BuildTabData m_UserData;
        private ToggleData m_ForceRebuild;
        private ToggleData m_CopyToStreaming;

        private AssetBundleSettings bundleSettings;

        internal AssetBundleBuildTab()
        {
            m_UserData = new BuildTabData
            {
                m_OnToggles = new List<string>(),
                m_UseDefaultPath = true
            };
        }

        internal void OnDisable()
        {
            var dataPath = Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserBuild.dat";

            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(dataPath);

            bf.Serialize(file, m_UserData);
            file.Close();

        }
        internal void OnEnable(EditorWindow parent)
        {
            m_InspectTab = (parent as AssetBundleBrowserMain).m_InspectTab;

            //LoadData...
            var dataPath = System.IO.Path.GetFullPath(".");
            dataPath = dataPath.Replace("\\", "/");
            dataPath += "/Library/AssetBundleBrowserBuild.dat";

            if (File.Exists(dataPath))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(dataPath, FileMode.Open);
                if (bf.Deserialize(file) is BuildTabData data)
                {
                    m_UserData = data;
                }

                file.Close();
            }

            m_ForceRebuild = new ToggleData(
             false,
             "Clear Folders",
             "Will wipe out all contents of build directory as well as StreamingAssets/AssetBundles if you are choosing to copy build there.",
             m_UserData.m_OnToggles);

            m_CopyToStreaming = new ToggleData(
                false,
                "Copy to StreamingAssets",
                "After build completes, will copy all build content to " + m_streamingPath + " for use in stand-alone player.",
                m_UserData.m_OnToggles);

            if (m_UserData.m_UseDefaultPath)
            {
                ResetPathToDefault();
            }
        }

        internal void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            EditorGUILayout.Space();
            GUILayout.BeginVertical();

            // User settings
            bundleSettings = (AssetBundleSettings)EditorGUILayout.ObjectField("Settings", bundleSettings, typeof(AssetBundleSettings), false);

            // Directory options
            bool state = false;
            using (new EditorGUI.DisabledScope(!AssetBundleModel.Model.DataSource.CanSpecifyBuildOutputDirectory))
            {
                EditorGUILayout.Space();
                state = GUILayout.Toggle(m_ForceRebuild.state, m_ForceRebuild.content);
                if (state != m_ForceRebuild.state)
                {
                    if (state)
                    {
                        m_UserData.m_OnToggles.Add(m_ForceRebuild.content.text);
                    }
                    else
                    {
                        _ = m_UserData.m_OnToggles.Remove(m_ForceRebuild.content.text);
                    }

                    m_ForceRebuild.state = state;
                }
                state = GUILayout.Toggle(m_CopyToStreaming.state, m_CopyToStreaming.content);
                if (state != m_CopyToStreaming.state)
                {
                    if (state)
                    {
                        m_UserData.m_OnToggles.Add(m_CopyToStreaming.content.text);
                    }
                    else
                    {
                        _ = m_UserData.m_OnToggles.Remove(m_CopyToStreaming.content.text);
                    }

                    m_CopyToStreaming.state = state;
                }
            }

            // Build
            EditorGUILayout.Space();
            if (GUILayout.Button("Build"))
            {
                EditorApplication.delayCall += ExecuteBuild;
            }

            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void ExecuteBuild()
        {
            string outputDirectory = bundleSettings.OutputDirectory;

            if (string.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = BrowseForFolder();
            }

            if (string.IsNullOrEmpty(outputDirectory)) //in case they hit "cancel" on the open browser
            {
                Debug.LogError("AssetBundle Build: No valid output path for build.");
                return;
            }

            if (m_ForceRebuild.state)
            {
                string message = "Do you want to delete all files in the directory " + outputDirectory;
                if (m_CopyToStreaming.state)
                {
                    message += " and " + m_streamingPath;
                }

                message += "?";
                if (EditorUtility.DisplayDialog("File delete confirmation", message, "Yes", "No"))
                {
                    try
                    {
                        if (Directory.Exists(outputDirectory))
                        {
                            Directory.Delete(outputDirectory, true);
                        }

                        if (m_CopyToStreaming.state)
                        {
                            if (Directory.Exists(m_streamingPath))
                            {
                                Directory.Delete(m_streamingPath, true);
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            if (!Directory.Exists(outputDirectory))
            {
                _ = Directory.CreateDirectory(outputDirectory);
            }

            BuildAssetBundleOptions opt = BuildAssetBundleOptions.None;
            switch (bundleSettings.Compress)
            {
                case AssetBundleSettings.CompressOptions.Uncompressed:
                    opt |= BuildAssetBundleOptions.UncompressedAssetBundle;
                    break;

                case AssetBundleSettings.CompressOptions.ChunkBasedCompression:
                    opt |= BuildAssetBundleOptions.ChunkBasedCompression;
                    break;

                case AssetBundleSettings.CompressOptions.StandardCompression:
                default:
                    break;
            }

            if (bundleSettings.ExcludeTypeInformation)
            {
                opt |= BuildAssetBundleOptions.DisableWriteTypeTree;
            }
            if (bundleSettings.ForceRebuild)
            {
                opt |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
            }
            if (bundleSettings.IgnoreTypeTreeChanges)
            {
                opt |= BuildAssetBundleOptions.IgnoreTypeTreeChanges;
            }
            if (bundleSettings.AppendHash)
            {
                opt |= BuildAssetBundleOptions.AppendHashToAssetBundleName;
            }
            if (bundleSettings.StrictMode)
            {
                opt |= BuildAssetBundleOptions.StrictMode;
            }
            if (bundleSettings.DryRunBuild)
            {
                opt |= BuildAssetBundleOptions.DryRunBuild;
            }

            ABBuildInfo buildInfo = new ABBuildInfo
            {
                OutputDirectory = outputDirectory,
                Options = opt,
                BuildTarget = bundleSettings.BuildTarget
            };
            buildInfo.OnBuild = (assetBundleName) =>
            {
                if (m_InspectTab == null)
                {
                    return;
                }

                m_InspectTab.AddBundleFolder(buildInfo.OutputDirectory);
                m_InspectTab.RefreshBundles();
            };

            _ = AssetBundleModel.Model.DataSource.BuildAssetBundles(buildInfo);

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            if (m_CopyToStreaming.state)
            {
                DirectoryCopy(outputDirectory, m_streamingPath);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                _ = Directory.CreateDirectory(destDirName);
            }

            foreach (string folderPath in Directory.GetDirectories(sourceDirName, "*", SearchOption.AllDirectories))
            {
                if (!Directory.Exists(folderPath.Replace(sourceDirName, destDirName)))
                {
                    _ = Directory.CreateDirectory(folderPath.Replace(sourceDirName, destDirName));
                }
            }

            foreach (string filePath in Directory.GetFiles(sourceDirName, "*.*", SearchOption.AllDirectories))
            {
                var fileDirName = Path.GetDirectoryName(filePath).Replace("\\", "/");
                var fileName = Path.GetFileName(filePath);
                string newFilePath = Path.Combine(fileDirName.Replace(sourceDirName, destDirName), fileName);

                File.Copy(filePath, newFilePath, true);
            }
        }

        private string BrowseForFolder()
        {
            m_UserData.m_UseDefaultPath = false;
            var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", m_UserData.m_OutputPath, string.Empty);
            if (!string.IsNullOrEmpty(newPath))
            {
                var gamePath = System.IO.Path.GetFullPath(".");
                gamePath = gamePath.Replace("\\", "/");
                if (newPath.StartsWith(gamePath) && newPath.Length > gamePath.Length)
                {
                    newPath = newPath.Remove(0, gamePath.Length + 1);
                }

                m_UserData.m_OutputPath = newPath;
                return newPath;
            }

            return null;
        }
        private void ResetPathToDefault()
        {
            m_UserData.m_UseDefaultPath = true;
            m_UserData.m_OutputPath = "AssetBundles/";
            m_UserData.m_OutputPath += m_UserData.m_BuildTarget.ToString();
        }

        //Note: this is the provided BuildTarget enum with some entries removed as they are invalid in the dropdown
        internal enum ValidBuildTarget
        {
            //NoTarget = -2,        --doesn't make sense
            //iPhone = -1,          --deprecated
            //BB10 = -1,            --deprecated
            //MetroPlayer = -1,     --deprecated
            StandaloneOSXUniversal = 2,
            StandaloneOSXIntel = 4,
            StandaloneWindows = 5,
            WebPlayer = 6,
            WebPlayerStreamed = 7,
            iOS = 9,
            PS3 = 10,
            XBOX360 = 11,
            Android = 13,
            StandaloneLinux = 17,
            StandaloneWindows64 = 19,
            WebGL = 20,
            WSAPlayer = 21,
            StandaloneLinux64 = 24,
            StandaloneLinuxUniversal = 25,
            WP8Player = 26,
            StandaloneOSXIntel64 = 27,
            BlackBerry = 28,
            Tizen = 29,
            PSP2 = 30,
            PS4 = 31,
            PSM = 32,
            XboxOne = 33,
            SamsungTV = 34,
            N3DS = 35,
            WiiU = 36,
            tvOS = 37,
            Switch = 38
        }

        [System.Serializable]
        internal class BuildTabData
        {
            internal List<string> m_OnToggles;
            internal ValidBuildTarget m_BuildTarget = ValidBuildTarget.StandaloneWindows;
            internal AssetBundleSettings.CompressOptions m_Compression = AssetBundleSettings.CompressOptions.StandardCompression;
            internal string m_OutputPath = string.Empty;
            internal bool m_UseDefaultPath = true;
        }
    }

}