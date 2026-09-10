using System.Collections.Generic;
using System.Linq;
using nickeltin.InternalBridge.Runtime;
using UnityEditor;

namespace nickeltin.InternalBridge.Editor
{
    public static class _AssetDatabase
    {
        public static EID GetMainAssetEID(string assetPath)
        {
#if UNITY_6000_3_OR_NEWER
            return AssetDatabase.GetMainAssetEntityId(assetPath);
#else
            return AssetDatabase.GetMainAssetInstanceID(assetPath);
#endif
        }
        
        public static IEnumerable<string> FindAssetsWithExtension(string ext, string[] inFolders = null)
        {
            ext = ext.Trim('.').ToLower();
            var filter = new SearchFilter
            {
                globs = new []{"*." + ext},
                skipHidden = false
            };

            if (inFolders == null || inFolders.Length == 0)
            {
                filter.searchArea = SearchFilter.SearchArea.AllAssets;
            }
            else
            {
                filter.searchArea = SearchFilter.SearchArea.SelectedFolders;
                filter.folders = inFolders;
            }
            
            return AssetDatabase.FindAllAssets(filter).Select(property => property.guid);
        }
    }
}