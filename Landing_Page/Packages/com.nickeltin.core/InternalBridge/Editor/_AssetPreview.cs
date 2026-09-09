using System;
using nickeltin.InternalBridge.Runtime;
using UnityEditor;
using UnityEngine;

namespace nickeltin.InternalBridge.Editor
{
    public static class _AssetPreview
    {
        public static Texture2D GetAssetPreview(EID eid)
        {
            return AssetPreview.GetAssetPreview(eid);
            // return null;
        }

        public static Texture2D GetMiniTypeThumbnailFromType(Type managedType)
        {
            return AssetPreview.GetMiniTypeThumbnailFromType(managedType);
        }
    }
}