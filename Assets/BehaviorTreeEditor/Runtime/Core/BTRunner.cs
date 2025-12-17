using System;
using UnityEngine;
using BehaviorTreeEditor.Runtime.Data;
using Sirenix.OdinInspector;

namespace BehaviorTreeEditor.Runtime.Core
{
    /// <summary>
    /// 行为树运行器 - 挂载到GameObject上运行行为树
    /// </summary>
    public class BTRunner : MonoBehaviour
    {
        [Title("行为树配置")]
        [LabelText("行为树资产")]
        [Required("请指定行为树资产")]
        [InlineEditor(InlineEditorModes.GUIOnly)]
        public BehaviorTreeSO behaviorTreeAsset;

        [LabelText("自动运行")]
        [Tooltip("是否在Start时自动开始运行")]
        public bool autoRun = true;

        [LabelText("更新模式")]
        public UpdateMode updateMode = UpdateMode.Update;

        [Title("运行时状态")]
        [LabelText("运行时树数据")]
        [ReadOnly]
        [ShowInInspector]
        public TreeData RuntimeTreeData { get; private set; }

        [LabelText("运行时黑板数据")]
        [ReadOnly]
        [ShowInInspector]
        public BlackboardData RuntimeBlackboard { get; private set; }

        [LabelText("当前状态")]
        [ReadOnly]
        [ShowInInspector]
        public NodeState CurrentState { get; private set; } = NodeState.Invalid;

        [LabelText("是否运行中")]
        [ReadOnly]
        [ShowInInspector]
        public bool IsRunning { get; private set; }

        // 运行时行为树实例
        private BehaviorTree runtimeTree;

        // 节点状态变化事件（供编辑器订阅）
        public event Action<string, NodeState> OnNodeStateChanged;

        // 树运行完成事件
        public event Action<NodeState> OnTreeCompleted;

        public enum UpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate,
            Manual
        }

        private void Start()
        {
            if (autoRun)
            {
                StartTree();
            }
        }

        private void Update()
        {
            if (updateMode == UpdateMode.Update && IsRunning)
            {
                Tick();
            }
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate && IsRunning)
            {
                Tick();
            }
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate && IsRunning)
            {
                Tick();
            }
        }

        /// <summary>
        /// 启动行为树
        /// </summary>
        [Button("启动行为树", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 0.4f)]
        public void StartTree()
        {
            if (behaviorTreeAsset == null)
            {
                Debug.LogError("[BTRunner] Behavior tree asset is not assigned!");
                return;
            }

            // 克隆数据，确保运行时修改不影响原始资产
            RuntimeTreeData = behaviorTreeAsset.treeData?.Clone();
            RuntimeBlackboard = behaviorTreeAsset.blackboardData?.Clone();

            if (RuntimeTreeData == null)
            {
                Debug.LogError("[BTRunner] Tree data is null!");
                return;
            }

            // 创建运行时行为树
            runtimeTree = new BehaviorTree();
            runtimeTree.Initialize(RuntimeTreeData, RuntimeBlackboard);

            // 订阅节点状态变化事件
            runtimeTree.OnNodeStateChanged += HandleNodeStateChanged;

            IsRunning = true;
            CurrentState = NodeState.Running;

            Debug.Log($"[BTRunner] Behavior tree started: {RuntimeTreeData.treeName}");
        }

        /// <summary>
        /// 停止行为树
        /// </summary>
        [Button("停止行为树", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        public void StopTree()
        {
            if (!IsRunning) return;

            if (runtimeTree != null)
            {
                runtimeTree.OnNodeStateChanged -= HandleNodeStateChanged;
                runtimeTree.Cleanup();
                runtimeTree = null;
            }

            IsRunning = false;
            CurrentState = NodeState.Invalid;

            Debug.Log("[BTRunner] Behavior tree stopped");
        }

        /// <summary>
        /// 重启行为树
        /// </summary>
        [Button("重启行为树")]
        public void RestartTree()
        {
            StopTree();
            StartTree();
        }

        /// <summary>
        /// 手动Tick（当UpdateMode为Manual时使用）
        /// </summary>
        [Button("手动Tick")]
        [ShowIf("@updateMode == UpdateMode.Manual")]
        public void ManualTick()
        {
            if (IsRunning)
            {
                Tick();
            }
        }

        /// <summary>
        /// 执行一次Tick
        /// </summary>
        private void Tick()
        {
            if (runtimeTree == null) return;

            CurrentState = runtimeTree.Tick();

            // 如果行为树执行完成（非Running状态）
            if (CurrentState != NodeState.Running)
            {
                OnTreeCompleted?.Invoke(CurrentState);
                
                // 可选：自动重启或停止
                // RestartTree(); // 循环执行
                // StopTree(); // 单次执行
            }
        }

        /// <summary>
        /// 处理节点状态变化
        /// </summary>
        private void HandleNodeStateChanged(BTNode node, NodeState state)
        {
            OnNodeStateChanged?.Invoke(node.NodeData?.guid, state);
        }

        /// <summary>
        /// 获取节点数据（接口方法）
        /// </summary>
        public NodeData GetNodeData(string guid)
        {
            return RuntimeTreeData?.GetNodeData(guid);
        }

        /// <summary>
        /// 获取树数据（接口方法）
        /// </summary>
        public TreeData GetTreeData()
        {
            return RuntimeTreeData?.GetTreeData();
        }

        /// <summary>
        /// 获取黑板数据
        /// </summary>
        public BlackboardData GetBlackboardData()
        {
            return RuntimeBlackboard;
        }

        /// <summary>
        /// 获取黑板变量值
        /// </summary>
        public T GetBlackboardValue<T>(string key)
        {
            return RuntimeBlackboard != null ? RuntimeBlackboard.GetValue<T>(key) : default;
        }

        /// <summary>
        /// 设置黑板变量值
        /// </summary>
        public void SetBlackboardValue<T>(string key, T value)
        {
            RuntimeBlackboard?.SetValue(key, value);
        }

        /// <summary>
        /// 获取当前运行的节点
        /// </summary>
        public BTNode GetCurrentRunningNode()
        {
            return runtimeTree?.CurrentRunningNode;
        }

        /// <summary>
        /// 获取所有节点实例（用于调试）
        /// </summary>
        public System.Collections.Generic.IEnumerable<BTNode> GetAllNodeInstances()
        {
            return runtimeTree?.GetAllNodes();
        }

        private void OnDestroy()
        {
            StopTree();
        }

        private void OnDisable()
        {
            if (!autoRun) return;
            StopTree();
        }

        private void OnEnable()
        {
            if (!autoRun || !Application.isPlaying) return;
            if (!IsRunning && behaviorTreeAsset != null)
            {
                StartTree();
            }
        }
    }
}