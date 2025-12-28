using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.BehaviourTree.Node.Composite;
using Framework.Common.BehaviourTree.Node.Decorator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.BehaviourTree.Editor.UI
{
    public class BehaviourTreeNodeView : UnityEditor.Experimental.GraphView.Node
    {
        public readonly Node.Node Node;
        public readonly Port Input;
        public readonly Port Output;

        public Action<BehaviourTreeNodeView> OnNodeSelected;
        public Action<BehaviourTreeNodeView> OnNodeUnselected;

        private readonly Label _labelAborted;

        public BehaviourTreeNodeView(Node.Node node) : base("Assets/Framework/Common/BehaviourTree/Editor/UI/BehaviourTreeNodeView.uxml")
        {
            Node = node;
            title = node.name;
            viewDataKey = node.guid;
            style.left = node.position.x;
            style.top = node.position.y;

            //数据映射与绑定
            var labelDescription = mainContainer.Q<Label>("description");
            var labelExecuteOrder = mainContainer.Q<Label>("executeOrder");
            _labelAborted = mainContainer.Q<Label>("aborted");
            labelDescription.text = node.Description;
            labelExecuteOrder.bindingPath = "executeOrder";
            labelExecuteOrder.Bind(new UnityEditor.SerializedObject(node));

            //输入端口
            if (Node is not RootNode)
            {
                Input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                Input.portName = "";
                Input.style.flexDirection = FlexDirection.Column;
                inputContainer.Add(Input);
            }

            switch (Node)
            {
                case RootNode rootNode:
                case DecoratorNode decoratorNode:
                    Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
                    break;
                case CompositeNode compositeNode:
                    Output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                    break;
            }
            if (Output != null)
            {
                Output.portName = "";
                Output.style.flexDirection = FlexDirection.Column;
                outputContainer.Add(Output);
            }

            SetupClasses();
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            Undo.RecordObject(Node, "Behaviour Tree Node(Set Position)");
            Node.position = new Vector2(newPos.xMin, newPos.yMin);
            EditorUtility.SetDirty(Node);
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected.Invoke(this);
        }
        public override void Unselect(VisualElement selectionContainer)
        {
            base.Unselect(selectionContainer);
            OnNodeUnselected.Invoke(this);
        }

        internal void SortChildren()
        {
            if (Node is CompositeNode compositeNode)
            {
                compositeNode.children.Sort((leftNode, rightNode) =>
                {
                    return leftNode.position.x <= rightNode.position.x ? -1 : 1;
                });
                for (int i = 0; i < compositeNode.children.Count; i++)
                {
                    var child = compositeNode.children[i];
                    child.executeOrder = i + 1;
                }
            }
        }

        internal void UpdateState()
        {
            if (Application.isPlaying)
            {
                RemoveFromClassList("running");
                RemoveFromClassList("success");
                RemoveFromClassList("failure");

                _labelAborted.style.display = Node.Aborted ? DisplayStyle.Flex : DisplayStyle.None;

                switch (Node.State)
                {
                    case NodeState.Running:
                        if (!Node.Started)
                        {
                            return;
                        }
                        AddToClassList("running");
                        break;
                    case NodeState.Success:
                        AddToClassList("success");
                        break;
                    case NodeState.Failure:
                        AddToClassList("failure");
                        break;
                }
            }
        }

        private void SetupClasses()
        {
            switch (Node)
            {
                case RootNode:
                    AddToClassList("root");
                    break;
                case ActionNode:
                    AddToClassList("action");
                    break;
                case DecoratorNode:
                    AddToClassList("decorator");
                    break;
                case CompositeNode:
                    AddToClassList("composite");
                    break;
            }
        }
    }
}
