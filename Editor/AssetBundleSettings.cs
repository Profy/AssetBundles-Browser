using UnityEditor;

using UnityEngine;

namespace AssetBundleBrowser
{
    [CreateAssetMenu(fileName = "AssetBundleSettings", menuName = "AssetBundle Browser/Asset Bundle Settings", order = 1)]
    internal class AssetBundleSettings : ScriptableObject
    {
        [Tooltip("Output asset bundle directory")]
        public string OutputDirectory = "Assets\\Editor\\Resources";

        [Tooltip("Choose target platform to build for.")]
        public BuildTarget BuildTarget =
#if UNITY_STANDALONE_WIN
            BuildTarget.StandaloneWindows64;
#elif UNITY_STANDALONE_LINUX
            BuildTarget.StandaloneLinux64;
#elif UNITY_STANDALONE_OSX
            BuildTarget.StandaloneOSX;
#endif

        [Tooltip("Choose no compress, standard (LZMA), or chunk based (LZ4)")]
        public CompressOptions Compress = CompressOptions.StandardCompression;
        [Tooltip("Do not include type information within the asset bundle (don't write type tree)")]
        public bool ExcludeTypeInformation = false;
        [Tooltip("Force rebuild the asset bundles")]
        public bool ForceRebuild = false;
        [Tooltip("Ignore the type tree changes when doing the incremental build check.")]
        public bool IgnoreTypeTreeChanges = false;
        [Tooltip("Append the hash to the assetBundle name.")]
        public bool AppendHash = false;
        [Tooltip("Do not allow the build to succeed if any errors are reporting during it.")]
        public bool StrictMode = false;
        [Tooltip("Do a dry run build.")]
        public bool DryRunBuild = false;

        internal enum CompressOptions
        {
            Uncompressed = 0,
            StandardCompression = 1,
            ChunkBasedCompression = 2,
        }
    }
}