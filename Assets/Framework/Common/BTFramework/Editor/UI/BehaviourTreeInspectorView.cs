using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.BehaviourTree.Editor.UI
{
    public class BehaviourTreeInspectorView : VisualElement
    {
        public new class UXmlFactory : UxmlFactory<BehaviourTreeInspectorView, VisualElement.UxmlTraits>
        {
        }
        //自定义NodeEditor
        private UnityEditor.Editor _nodeEditor;

        public BehaviourTreeInspectorView()
        {
        }

        internal void HandleNodeSelected(BehaviourTreeNodeView nodeView)
        {
            ClearNode();

            var scrollView = new ScrollView();
            _nodeEditor = UnityEditor.Editor.CreateEditor(nodeView.Node);//使用默认对象编辑GUI绘制
            //混合使用IMGUI UIToolkit
            var container = new IMGUIContainer(() =>
            {
                if (_nodeEditor != null&&_nodeEditor.target)
                {
                    _nodeEditor.OnInspectorGUI();
                }
            });
            scrollView.Add(container);
            Add(scrollView);
        }
        internal void HandleNodeUnselected(BehaviourTreeNodeView nodeView)
        {
            if (_nodeEditor.target == nodeView.Node)
            {
                ClearNode();
            }
        }
        private void ClearNode()
        {
            Clear();
            if (_nodeEditor)
            {
                GameObject.DestroyImmediate(_nodeEditor);
                _nodeEditor = null;
            }
        }
    }
}