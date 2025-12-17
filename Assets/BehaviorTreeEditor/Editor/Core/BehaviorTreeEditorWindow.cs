using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor.Callbacks;

namespace BehaviorTreeEditor.Editor.Core
{
    /// <summary>
    /// 行为树编辑器主窗口 - 使用UIToolkit实现
    /// </summary>
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        // UI组件引用
        private VisualElement root;
        private VisualElement topBar;
        private VisualElement middleContainer;
        private VisualElement leftPanel;
        private VisualElement leftTopPanel;
        private VisualElement leftBottomPanel;
        private VisualElement rightPanel;
        
        // 核心组件
        private BehaviorTreeGraphView graphView;
        private IMGUIContainer blackboardContainer;
        private IMGUIContainer nodeInspectorContainer;
        
        // 数据
        private BehaviorTreeSO currentAsset;
        private BTNodeView selectedNodeView;
        private bool hasUnsavedChanges = false;
        
        // Odin PropertyTree
        private PropertyTree blackboardPropertyTree;
        private PropertyTree nodePropertyTree;
        
        // 视图模式
        private bool isRuntimeMode = false;
        private BTRunner currentRunner;
        
        // 加载标志，防止在加载过程中误标记为未保存
        private bool isLoading = false;

        [MenuItem("Tools/Behavior Tree/Editor Window #s")]
        public static void OpenWindow()
        {
            var window = GetWindow<BehaviorTreeEditorWindow>();
            window.titleContent = new GUIContent("Behavior Tree Editor");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Selection.selectionChanged -= OnSelectionChanged;
            
            // 检查未保存的更改
            if (hasUnsavedChanges && currentAsset != null)
            {
                if (EditorUtility.DisplayDialog("未保存的更改", 
                    $"行为树 {currentAsset.name} 有未保存的更改，是否保存？", "保存", "不保存"))
                {
                    SaveCurrentAsset();
                }
            }
            
            blackboardPropertyTree?.Dispose();
            nodePropertyTree?.Dispose();
        }

        private void CreateGUI()
        {
            root = rootVisualElement;
            
            // 加载样式
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/BehaviorTreeEditor/Editor/USS/BehaviorTreeEditor.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            CreateTopBar();
            CreateMiddleContainer();
            ApplyDefaultStyles();
            
            // 初始状态：没有加载任何资产，所以没有未保存的更改
            hasUnsavedChanges = false;
            UpdateUnsavedIndicator();
        }

        private void CreateTopBar()
        {
            topBar = new VisualElement();
            topBar.name = "top-bar";
            topBar.style.height = 30;
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            topBar.style.paddingLeft = 5;
            topBar.style.paddingRight = 5;
            topBar.style.alignItems = Align.Center;

            // 打开按钮
            var openButton = new Button(OnOpenClicked) { text = "打开" };
            openButton.style.width = 60;
            topBar.Add(openButton);

            // 新建按钮
            var newButton = new Button(OnNewClicked) { text = "新建" };
            newButton.style.width = 60;
            topBar.Add(newButton);

            // 保存按钮
            var saveButton = new Button(OnSaveClicked) { text = "保存" };
            saveButton.style.width = 60;
            topBar.Add(saveButton);

            // 清空按钮
            var clearButton = new Button(OnClearClicked) { text = "清空" };
            clearButton.style.width = 60;
            topBar.Add(clearButton);

            // 分隔
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            topBar.Add(spacer);

            // 当前资产标签
            var assetLabel = new Label("当前: 无");
            assetLabel.name = "asset-label";
            assetLabel.style.color = Color.white;
            topBar.Add(assetLabel);

            // 未保存标记 - 修改为灰色小圆点
            var unsavedLabel = new Label("●");
            unsavedLabel.name = "unsaved-label";
            unsavedLabel.style.color = Color.gray;
            unsavedLabel.style.fontSize = 10; // 变小
            unsavedLabel.style.marginLeft = 5;
            unsavedLabel.style.marginTop = 2; // 微调位置
            unsavedLabel.style.display = DisplayStyle.None;
            topBar.Add(unsavedLabel);

            // 分隔
            var spacer2 = new VisualElement();
            spacer2.style.flexGrow = 1;
            topBar.Add(spacer2);

            // 模式切换
            var modeToggle = new Toggle("Runtime模式");
            modeToggle.name = "mode-toggle";
            modeToggle.RegisterValueChangedCallback(evt => OnModeChanged(evt.newValue));
            topBar.Add(modeToggle);

            root.Add(topBar);
        }

        private void CreateMiddleContainer()
        {
            middleContainer = new VisualElement();
            middleContainer.name = "middle-container";
            middleContainer.style.flexDirection = FlexDirection.Row;
            middleContainer.style.flexGrow = 1;

            CreateLeftPanel();
            CreateRightPanel();
            root.Add(middleContainer);
        }

        private void CreateLeftPanel()
        {
            leftPanel = new VisualElement();
            leftPanel.name = "left-panel";
            leftPanel.style.width = new StyleLength(new Length(30, LengthUnit.Percent));
            leftPanel.style.flexDirection = FlexDirection.Column;
            leftPanel.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            // 上部：黑板数据面板
            leftTopPanel = new VisualElement();
            leftTopPanel.name = "left-top-panel";
            leftTopPanel.style.height = new StyleLength(new Length(50, LengthUnit.Percent));
            leftTopPanel.style.borderBottomWidth = 1;
            leftTopPanel.style.borderBottomColor = new Color(0.1f, 0.1f, 0.1f);

            var blackboardHeader = new Label("黑板数据 (Blackboard)");
            blackboardHeader.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            blackboardHeader.style.paddingLeft = 10;
            blackboardHeader.style.paddingTop = 5;
            blackboardHeader.style.paddingBottom = 5;
            blackboardHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftTopPanel.Add(blackboardHeader);

            blackboardContainer = new IMGUIContainer(DrawBlackboardInspector);
            blackboardContainer.style.flexGrow = 1;
            leftTopPanel.Add(blackboardContainer);

            leftPanel.Add(leftTopPanel);

            // 下部：节点配置面板
            leftBottomPanel = new VisualElement();
            leftBottomPanel.name = "left-bottom-panel";
            leftBottomPanel.style.flexGrow = 1;

            var nodeHeader = new Label("节点配置 (Node Inspector)");
            nodeHeader.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
            nodeHeader.style.paddingLeft = 10;
            nodeHeader.style.paddingTop = 5;
            nodeHeader.style.paddingBottom = 5;
            nodeHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftBottomPanel.Add(nodeHeader);

            nodeInspectorContainer = new IMGUIContainer(DrawNodeInspector);
            nodeInspectorContainer.style.flexGrow = 1;
            leftBottomPanel.Add(nodeInspectorContainer);

            leftPanel.Add(leftBottomPanel);
            middleContainer.Add(leftPanel);
        }

        private void CreateRightPanel()
        {
            rightPanel = new VisualElement();
            rightPanel.name = "right-panel";
            rightPanel.style.flexGrow = 1;

            graphView = new BehaviorTreeGraphView(this);
            graphView.name = "behavior-tree-graph";
            graphView.style.flexGrow = 1;

            rightPanel.Add(graphView);
            middleContainer.Add(rightPanel);
        }

        private void ApplyDefaultStyles()
        {
            root.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        }

        private void DrawBlackboardInspector()
        {
            if (currentAsset == null || currentAsset.blackboardData == null)
            {
                EditorGUILayout.HelpBox("请先打开一个行为树资产", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            
            if (blackboardPropertyTree == null || blackboardPropertyTree.TargetType != typeof(BlackboardData))
            {
                blackboardPropertyTree?.Dispose();
                blackboardPropertyTree = PropertyTree.Create(currentAsset.blackboardData);
            }

            blackboardPropertyTree.Draw(false);

            if (EditorGUI.EndChangeCheck() && !isLoading)
            {
                MarkUnsavedChanges();
            }
        }

        // 当双击资产文件时，打开行为树编辑器
        [OnOpenAsset()]
        public static bool OnDoubleClick(int instanceID)
        {
            // 获取窗口实例并打开
            BehaviorTreeEditorWindow wnd = GetWindow<BehaviorTreeEditorWindow>();
            if (wnd == null)
            {
                OpenWindow();
                wnd = GetWindow<BehaviorTreeEditorWindow>();
            }
            wnd.RemoveNotification();

            // 获取资产路径
            string fullPath = AssetDatabase.GetAssetPath(instanceID);

            // 检测是否目标资产类型
            BehaviorTreeSO btreeData = AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(fullPath);
            if (btreeData == null)
            {
                return false;
            }

            // 如果有未保存的更改，提示用户
            if (wnd.hasUnsavedChanges)
            {
                string str = "确认打开新行为树并覆盖当前视图内容吗？未保存的数据将无法恢复。";
                if (!EditorUtility.DisplayDialog("警告", str, "确认", "取消"))
                {
                    return true; // 用户取消，不打开新文件
                }
            }

            wnd.LoadBehaviorTree(btreeData);

            // 提示消息
            string message = $"文件已打开";
            wnd.ShowNotification(new GUIContent(message));

            return true;
        }

        private void DrawNodeInspector()
        {
            if (selectedNodeView == null || selectedNodeView.NodeData == null)
            {
                EditorGUILayout.HelpBox("请选择一个节点", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();

            if (nodePropertyTree == null)
            {
                nodePropertyTree?.Dispose();
                nodePropertyTree = PropertyTree.Create(selectedNodeView.NodeData);
            }

            EditorGUILayout.LabelField($"节点: {selectedNodeView.NodeData.nodeName}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            nodePropertyTree.Draw(false);

            if (EditorGUI.EndChangeCheck() && !isLoading)
            {
                selectedNodeView.UpdateFromData();
                MarkUnsavedChanges();
            }
        }

        #region 按钮回调

        private void OnOpenClicked()
        {
            if (hasUnsavedChanges && !ConfirmDiscardChanges()) return;

            string path = EditorUtility.OpenFilePanel("打开行为树", "Assets", "asset");
            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
                var asset = AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(path);
                if (asset != null)
                {
                    LoadBehaviorTree(asset);
                }
            }
        }

        private void OnNewClicked()
        {
            if (hasUnsavedChanges && !ConfirmDiscardChanges()) return;

            string path = EditorUtility.SaveFilePanelInProject("新建行为树", "NewBehaviorTree", "asset", "");
            if (!string.IsNullOrEmpty(path))
            {
                var asset = CreateInstance<BehaviorTreeSO>();
                asset.treeData = TreeData.Create("New Behavior Tree");
                asset.blackboardData = new BlackboardData();
                
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                
                LoadBehaviorTree(asset);
                // 新建文件后不立即标记为未保存，等待用户实际修改
            }
        }

        private void OnSaveClicked()
        {
            SaveCurrentAsset();
        }

        private void OnClearClicked()
        {
            if (currentAsset != null && EditorUtility.DisplayDialog("确认", "确定要清空当前视图吗？", "确定", "取消"))
            {
                graphView?.ClearGraph();
                MarkUnsavedChanges();
            }
        }

        private void OnModeChanged(bool isRuntime)
        {
            isRuntimeMode = isRuntime;
            
            if (isRuntime)
            {
                if (Application.isPlaying)
                {
                    FindAndBindRunner();
                }
                else
                {
                    Debug.LogWarning("[BehaviorTreeEditor] Runtime模式仅在播放模式下可用");
                    var toggle = root.Q<Toggle>("mode-toggle");
                    toggle?.SetValueWithoutNotify(false);
                    isRuntimeMode = false;
                }
            }
            else
            {
                UnbindRunner();
            }

            graphView?.SetRuntimeMode(isRuntimeMode);
        }

        #endregion

        #region 资产管理

        /// <summary>
        /// 加载行为树资产
        /// </summary>
        public void LoadBehaviorTree(BehaviorTreeSO asset)
        {
            isLoading = true; // 开始加载，防止误触发未保存标记
            
            try
            {
                // 在加载前先清空图形视图
                graphView?.ClearGraph();
                
                currentAsset = asset;
                hasUnsavedChanges = false; // 加载现有资产时重置为已保存状态
                selectedNodeView = null;
                
                // 更新UI
                UpdateAssetLabel();
                UpdateUnsavedIndicator();

                // 重置PropertyTree
                blackboardPropertyTree?.Dispose();
                blackboardPropertyTree = null;
                nodePropertyTree?.Dispose();
                nodePropertyTree = null;

                // 加载到GraphView
                graphView?.LoadFromAsset(asset);
            }
            finally
            {
                isLoading = false; // 加载完成
            }
        }

        /// <summary>
        /// 设置选中的节点
        /// </summary>
        public void SetSelectedNode(BTNodeView nodeView)
        {
            selectedNodeView = nodeView;
            
            nodePropertyTree?.Dispose();
            nodePropertyTree = null;
            
            if (nodeView?.NodeData != null)
            {
                nodePropertyTree = PropertyTree.Create(nodeView.NodeData);
            }
        }

        /// <summary>
        /// 标记有未保存的更改
        /// </summary>
        public void MarkUnsavedChanges()
        {
            if (!hasUnsavedChanges && !isLoading)
            {
                hasUnsavedChanges = true;
                UpdateUnsavedIndicator();
            }
        }

        /// <summary>
        /// 保存当前资产
        /// </summary>
        private void SaveCurrentAsset()
        {
            if (currentAsset != null)
            {
                graphView?.SaveToAsset();
                EditorUtility.SetDirty(currentAsset);
                AssetDatabase.SaveAssets();
                
                hasUnsavedChanges = false;
                UpdateUnsavedIndicator();
                Debug.Log($"[BehaviorTreeEditor] Saved: {currentAsset.name}");
            }
        }

        private void UpdateAssetLabel()
        {
            var label = root.Q<Label>("asset-label");
            if (label != null)
            {
                label.text = currentAsset != null ? $"当前: {currentAsset.name}" : "当前: 无";
            }
        }

        private void UpdateUnsavedIndicator()
        {
            var unsavedLabel = root.Q<Label>("unsaved-label");
            if (unsavedLabel != null)
            {
                unsavedLabel.style.display = hasUnsavedChanges ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private bool ConfirmDiscardChanges()
        {
            return EditorUtility.DisplayDialog("未保存的更改", 
                "当前有未保存的更改，确定要放弃吗？", "放弃", "取消");
        }

        #endregion

        #region Runtime模式

        private void FindAndBindRunner()
        {
            var runners = FindObjectsOfType<BTRunner>();
            if (runners.Length > 0)
            {
                var selected = Selection.activeGameObject?.GetComponent<BTRunner>();
                currentRunner = selected ?? runners[0];
                
                if (currentRunner != null)
                {
                    currentRunner.OnNodeStateChanged += OnRuntimeNodeStateChanged;
                    Debug.Log($"[BehaviorTreeEditor] Bound to runner: {currentRunner.name}");
                }
            }
        }

        private void UnbindRunner()
        {
            if (currentRunner != null)
            {
                currentRunner.OnNodeStateChanged -= OnRuntimeNodeStateChanged;
                currentRunner = null;
            }
        }

        private void OnRuntimeNodeStateChanged(string guid, NodeState state)
        {
            graphView?.UpdateNodeHighlight(guid, state);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && isRuntimeMode)
            {
                FindAndBindRunner();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                UnbindRunner();
                var toggle = root.Q<Toggle>("mode-toggle");
                toggle?.SetValueWithoutNotify(false);
                isRuntimeMode = false;
            }
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is BehaviorTreeSO btAsset)
            {
                if (hasUnsavedChanges && !ConfirmDiscardChanges()) return;
                LoadBehaviorTree(btAsset);
            }
        }

        #endregion
    }
}