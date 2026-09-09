using nickeltin.InternalBridge.Editor;
using nickeltin.InternalBridge.Runtime;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace nickeltin.Core.Editor
{
    public static class AssetCreator
    {
        public class CreateAssetWithContent :
#if UNITY_6000_4_OR_NEWER
            AssetCreationEndAction
#else
            EndNameEditAction
#endif
        {
            public string FileContent;
            public ObjectCreatedDelegate ObjectCreated;
        
#if UNITY_6000_4_OR_NEWER
            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                Action_Internal(entityId, pathName, resourceFile);
            }
#else
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Action_Internal(instanceId, pathName, resourceFile);
            }
#endif

            private void Action_Internal(EID eid, string pathName, string resourceFile)
            {
                var instance = _ProjectWindowUtil.CreateScriptAssetWithContent(pathName, FileContent);
                ProjectWindowUtil.ShowCreatedAsset(instance);
                ObjectCreated?.Invoke(instance, pathName);
            }
        }
        
        public class CreateAssetFromObject : 
#if UNITY_6000_4_OR_NEWER
            AssetCreationEndAction
#else
            EndNameEditAction
#endif
        {
            public ObjectCreatedDelegate ObjectCreated;
            public ObjectCreatorDelegate ObjectCreator;
        
#if UNITY_6000_4_OR_NEWER
            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                Action_Internal(entityId, pathName, resourceFile);
            }
#else
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                Action_Internal(instanceId, pathName, resourceFile);
            }
#endif
            private void Action_Internal(EID eid, string pathName, string resourceFile)
            {
                var instance = ObjectCreator();
                // ProjectWindowUtil.CreateAsset(instance, pathName);
                // Debug.Log(pathName);
                InternalEditorUtility.SaveToSerializedFileAndForget(new [] {instance}, pathName, EditorSettings.serializationMode != SerializationMode.ForceBinary);
                AssetDatabase.ImportAsset(pathName);
                ProjectWindowUtil.ShowCreatedAsset(instance);
                ObjectCreated?.Invoke(instance, pathName);
            }
        }
        
        public delegate void ObjectCreatedDelegate(Object @object, string path); 
        public delegate Object ObjectCreatorDelegate(); 
        
        public static void CreateWithContent(string filename, string content, ObjectCreatedDelegate objectCreated)
        {
            var instance = ScriptableObject.CreateInstance<CreateAssetWithContent>();
            instance.FileContent = content;
            instance.ObjectCreated = objectCreated;
            StartNameEditingIfProjectWindowExists(instance, filename, null, null);
        }

        public static void CreateFromObject(string filename, ObjectCreatorDelegate objectCreator,
            ObjectCreatedDelegate objectCreated)
        {
            var instance = ScriptableObject.CreateInstance<CreateAssetFromObject>();
            instance.ObjectCreator = objectCreator;
            instance.ObjectCreated = objectCreated;
            StartNameEditingIfProjectWindowExists(instance, filename, null, null);
        }
        
        private static void StartNameEditingIfProjectWindowExists(
#if UNITY_6000_4_OR_NEWER
            AssetCreationEndAction
#else
            EndNameEditAction
#endif 
            endAction, string pathName, Texture2D icon, string resourceFile)
        {
#if UNITY_6000_4_OR_NEWER
            var id = EntityId.None;
#else
            var id = 0;
#endif
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(id, endAction, pathName, icon, resourceFile, true);
        }
    }
}