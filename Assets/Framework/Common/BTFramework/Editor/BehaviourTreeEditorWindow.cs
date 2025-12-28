using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Editor
{
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Open BehaviourTreeEditorWindow")]
        public static void OpenBehaviourTreeEditorWindow()
        {
            var window = GetWindow<BehaviourTreeEditorWindow>();
            window.titleContent = new GUIContent("Behaviour Tree Editor");
        }

        //双击资产时节点函数回调
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId,int line)
        {
            if(Selection.activeObject is BehaviourTree behaviourTree && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                OpenBehaviourTreeEditorWindow();
                return true;
            }
            return false;
        }

        
    }

}
