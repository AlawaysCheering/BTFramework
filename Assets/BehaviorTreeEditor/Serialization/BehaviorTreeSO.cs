using UnityEngine;
using BehaviorTreeEditor.Runtime.Data;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System;

namespace BehaviorTreeEditor.Runtime.Core
{
    /// <summary>
    /// 行为树ScriptableObject资产
    /// 继承SerializedScriptableObject以支持复杂类型序列化
    /// </summary>
    [CreateAssetMenu(fileName = "NewBehaviorTree", menuName = "Behavior Tree/Behavior Tree Asset", order = 1)]
    public class BehaviorTreeSO : SerializedScriptableObject
    {
        [Title("行为树数据")]
        [OdinSerialize,NonSerialized]
        public TreeData treeData;

        [Title("黑板数据")]
        [HideLabel]
        [InlineProperty]
        public BlackboardData blackboardData;

        /// <summary>
        /// 创建时初始化
        /// </summary>
        private void OnEnable()
        {
            if (treeData == null)
            {
                treeData = TreeData.Create(name);
            }
            if (blackboardData == null)
            {
                blackboardData = new BlackboardData();
            }
        }

        /// <summary>
        /// 验证资产
        /// </summary>
        [Button("验证行为树", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.6f, 0.8f)]
        public void Validate()
        {
            if (treeData == null)
            {
                Debug.LogError($"[{name}] TreeData is null!");
                return;
            }

            if (treeData.Validate(out string error))
            {
                Debug.Log($"[{name}] Behavior tree is valid!");
            }
            else
            {
                Debug.LogError($"[{name}] Validation failed: {error}");
            }
        }

        /// <summary>
        /// 重置为空树
        /// </summary>
        [Button("重置行为树")]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        public void ResetTree()
        {
            treeData = TreeData.Create(name);
            blackboardData = new BlackboardData();
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        /// <summary>
        /// 获取节点数据
        /// </summary>
        public NodeData GetNodeData(string guid)
        {
            return treeData?.GetNodeData(guid);
        }

        /// <summary>
        /// 获取树数据
        /// </summary>
        public TreeData GetTreeData()
        {
            return treeData?.GetTreeData();
        }

        /// <summary>
        /// 克隆为运行时数据
        /// </summary>
        public (TreeData, BlackboardData) CloneForRuntime()
        {
            return (treeData?.Clone(), blackboardData?.Clone());
        }
    }
}