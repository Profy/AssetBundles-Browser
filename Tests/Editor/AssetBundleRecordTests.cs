using AssetBundleBrowser;

using NUnit.Framework;

using UnityEngine;

namespace AssetBundleBrowserTests
{
    public class AssetBundleRecordTests
    {
        [TestCase]
        public void TestAssetBundleRecordConstructor()
        {
            VerifyConstructorException(null, null);
            VerifyConstructorException(string.Empty, null);
            VerifyConstructorException("bundleName.one", null);
        }

        private void VerifyConstructorException(string path, AssetBundle bundle)
        {
            _ = Assert.Throws<System.ArgumentException>(() =>
            {
                _ = new AssetBundleRecord(path, bundle);
            });
        }
    }
}
