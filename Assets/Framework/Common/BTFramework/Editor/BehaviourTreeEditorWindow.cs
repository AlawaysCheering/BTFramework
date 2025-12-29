using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Common.BehaviourTree.Editor.UI;
using Framework.Common.Blackboard.Editor.UI;
using Framework.Common.Debug;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Framework.Common.BehaviourTree.Editor
{
    /// <summary>
    /// 行为树编辑器窗口类
    /// 提供一个可视化界面用于编辑和查看行为树
    /// </summary>
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        // UI组件引用
        private BehaviourTreeGraphView _graphView;            // 图形化节点视图
        private BehaviourTreeInspectorView _inspectorView;    // 节点属性检查器视图
        private BlackboardView _blackboardView;              // 黑板数据视图
        private ObjectField _ofBehaviourTree;                 // 行为树资源选择字段
        private DropdownField _dfOperationPath;              // 操作路径下拉菜单（用于导航嵌套行为树）
        private ToolbarButton _tbRevert;                     // 撤销按钮
        private ToolbarButton _tbApply;                      // 应用按钮

        // 存储操作路径的栈，用于导航嵌套行为树
        private readonly Stack<BehaviourTree> _operationPath = new();

        /// <summary>
        /// 通过菜单打开编辑器窗口
        /// </summary>
        [MenuItem("Tools/Behaviour Tree Editor")]
        public static void ShowBehaviourTreeEditorWindow()
        {
            // 获取或创建编辑器窗口
            var window = GetWindow<BehaviourTreeEditorWindow>();
            // 设置窗口标题
            window.titleContent = new GUIContent("Behaviour Tree Editor");
        }

        /// <summary>
        /// 双击资源时节点函数回调
        /// 在资源管理器双击行为树资源时自动打开编辑器窗口
        /// </summary>
        /// <param name="instanceId">资源的实例ID</param>
        /// <param name="line">行号（在脚本资源中有效）</param>
        /// <returns>返回true表示已处理该事件，阻止Unity默认行为</returns>
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            // 检查选中的对象是否为行为树资源
            if (Selection.activeObject is BehaviourTree behaviourTree && AssetDatabase.CanOpenAssetInEditor(instanceId))
            {
                // 显示编辑器窗口
                ShowBehaviourTreeEditorWindow();
                return true;  // 已处理，阻止Unity默认打开方式
            }

            return false;  // 未处理，由Unity使用默认方式打开
        }

        /// <summary>
        /// 创建GUI界面
        /// Unity在窗口创建时调用此方法
        /// </summary>
        public void CreateGUI()
        {
            var root = rootVisualElement;

            // 加载并实例化UXML布局文件
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Framework/Common/BTFramework/Editor/BehaviourTreeEditorWindow.uxml");
            visualTree.CloneTree(root);

            // 加载并应用样式表
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Framework/Common/BTFramework/Editor/BehaviourTreeEditorWindow.uss");
            root.styleSheets.Add(styleSheet);

            // 获取UI组件引用
            _graphView = root.Q<BehaviourTreeGraphView>("BehaviourTreeGraphView");
            _inspectorView = root.Q<BehaviourTreeInspectorView>("BehaviourTreeInspectorView");
            _blackboardView = root.Q<BlackboardView>("BehaviourTreeBlackboardView");
            _ofBehaviourTree = root.Q<ObjectField>("OfBehaviourTree");
            _dfOperationPath = root.Q<DropdownField>("DfOperationPath");
            _tbRevert = root.Q<ToolbarButton>("TbRevert");
            _tbApply = root.Q<ToolbarButton>("TbApply");

            // 清空操作路径栈
            _operationPath.Clear();

            // 设置图形视图的事件回调
            // 当节点被选中时，更新检查器视图
            _graphView.OnNodeSelected = _inspectorView.HandleNodeSelected;
            // 当节点取消选中时，清空检查器视图
            _graphView.OnNodeUnselected = _inspectorView.HandleNodeUnselected;

            // 注册播放模式状态改变事件
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            // 注册行为树资源改变事件
            _ofBehaviourTree.RegisterValueChangedCallback(HandleBehaviourTreeChanged);

            // 注册操作路径选择改变事件
            _dfOperationPath.RegisterValueChangedCallback(HandleOperationPathChanged);

            // 注册工具栏按钮点击事件
            _tbRevert.clicked += HandleRevertClicked;
            _tbApply.clicked += HandleApplyClicked;

            // 尝试获取并显示当前选中的行为树
            TryToGetTree();
            // 更新视图以显示获取到的行为树
            TryToUpdateView();
        }

        private void Update()
        {
            _graphView?.UpdateNodeStates();
            if (_ofBehaviourTree.value && _ofBehaviourTree.value is BehaviourTree)
            {
                _tbRevert.SetEnabled(true);
                _tbApply.SetEnabled(true);
            }
            else
            {
                _tbRevert.SetEnabled(false);
                _tbApply.SetEnabled(false);
            }
        }

        private void OnGUI()
        {
            if (_graphView != null && Event.current != null)
            {
                _graphView.MousePosition = Event.current.mousePosition;
            }
        }

        private void OnDestroy()
        {
            _graphView.OnNodeSelected = null;
            _graphView.OnNodeUnselected = null;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            _ofBehaviourTree.UnregisterValueChangedCallback(HandleBehaviourTreeChanged);
            _tbRevert.clicked -= HandleRevertClicked;
            _tbApply.clicked -= HandleApplyClicked;
        }

        /// <summary>
        /// 项目资源选中改变节点回调
        /// </summary>
        private void OnSelectionChange()
        {
            // 获取行为树
            TryToGetTree();
        }

        private void TryToGetTree()
        {
            // 当项目选中行为树文件且能够打开行为树时获取行为树
            if (Selection.activeObject is BehaviourTree behaviourTree)
            {
                _ofBehaviourTree.value = behaviourTree;
                return;
            }

            // 当项目选中游戏对象且存在行为树运行组件且树不为空时获取行为树
            if (Selection.activeGameObject)
            {
                var behaviourTreeExecutor = Selection.activeGameObject.GetComponent<BehaviourTreeExecutor>();
                if (behaviourTreeExecutor && behaviourTreeExecutor.RuntimeTree)
                {
                    _ofBehaviourTree.value = behaviourTreeExecutor.RuntimeTree;
                }
            }
        }

        private void TryToUpdateView()
        {
            var tree = _ofBehaviourTree.value as BehaviourTree;
            _graphView?.UpdateView(tree);
            _blackboardView?.UpdateView(tree?.blackboard);
            UpdateOperationPath();
        }

        private void UpdateOperationPath()
        {
            var behaviourTree = _ofBehaviourTree.value as BehaviourTree;
            if (behaviourTree == null)
            {
                // 清空路径
                _operationPath.Clear();
                _dfOperationPath.value = null;
                _dfOperationPath.index = -1;
            }
            else
            {
                if (_operationPath.Contains(behaviourTree)) // 如果当前路径包含该树，认为是回退操作
                {
                    while (_operationPath.TryPeek(out var tree))
                    {
                        if (tree == behaviourTree)
                        {
                            break;
                        }

                        _operationPath.Pop();
                    }

                    _dfOperationPath.choices = _operationPath.Select(tree => tree.name).Reverse().ToList();
                    _dfOperationPath.index = _operationPath.Count - 1;
                }
                else // 否则认为是压入操作
                {
                    // 在此之前判断路径是否存在操作且树是否为路径最后一个操作的子树，是则追加，否则清空再添加
                    if (_operationPath.Count == 0 || _operationPath.Peek() != behaviourTree.Parent)
                    {
                        _operationPath.Clear();
                    }

                    _operationPath.Push(behaviourTree);
                    _dfOperationPath.choices = _operationPath.Select(tree => tree.name).Reverse().ToList();
                    _dfOperationPath.index = _operationPath.Count - 1;
                }
            }
        }

        private void HandlePlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    // 获取行为树
                    TryToGetTree();
                    TryToUpdateView();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    // 获取行为树
                    TryToGetTree();
                    TryToUpdateView();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        private void HandleBehaviourTreeChanged(ChangeEvent<Object> evt)
        {
            // 更新树UI
            TryToUpdateView();
        }

        private void HandleOperationPathChanged(ChangeEvent<string> evt)
        {
            if (_dfOperationPath.index != -1)
            {
                while (_operationPath.TryPeek(out var tree))
                {
                    if (tree.name == _dfOperationPath.value)
                    {
                        break;
                    }

                    _operationPath.Pop();
                }

                Selection.activeObject = _operationPath.Count == 0 ? null : _operationPath.Peek();
            }
        }

        private void HandleRevertClicked()
        {
            Undo.PerformUndo();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void HandleApplyClicked()
        {
            if (_ofBehaviourTree.value && _ofBehaviourTree.value is BehaviourTree tree)
            {
                Undo.ClearAll();
                new SerializedObject(tree).ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }
    }
}