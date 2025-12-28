using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Composite;
using Framework.Common.BehaviourTree.Node.Decorator;
using Framework.Core.Attribute;
using ParadoxNotion.Design;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


namespace Framework.Common.BehaviourTree
{
    public enum TreeState
    {
        Initialized,
        Running,
        Success,
        Failure,
    }

    [CreateAssetMenu(menuName ="Behaviour Tree")]
    public class BehaviourTree : ScriptableObject
    {
        [DisplayOnly] public Node.Node rootNode;
        [DisplayOnly] public TreeState treeState = TreeState.Initialized;
        [DisplayOnly] public List<Node.Node> nodes = new();
        [DisplayOnly] public Blackboard.Blackboard blackboard;

        [NonSerialized] public BehaviourTree Parent; //运行时关联的父树，仅在子树节点中设置
        [NonSerialized] public float Time;//运行时时间

#if UNITY_EDITOR
        public virtual void CreateBlackboard()
        {
            var blackboard = ScriptableObject.CreateInstance<Blackboard.Blackboard>();
            blackboard.name = "Blackboard";
            if (!Application.isPlaying)// 文件附加到另一资源文件需要在非运行状态才能执行
            {
                AssetDatabase.AddObjectToAsset(blackboard, this);//关联父资产
            }
            this.blackboard=blackboard;
            Dfs(rootNode, node => node.blackboard = blackboard);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public virtual Node.Node CreateRootNode()
        {
            return CreateNode(typeof(RootNode));
        }

        public Node.Node CreateNode(Type type)
        {
            Undo.RecordObject(this, "Behaviour Tree(Create Node)");
            var node = ScriptableObject.CreateInstance(type) as Node.Node;
            node.name = type.Name;
            node.guid = GUID.Generate().ToString();
            node.blackboard = blackboard;
            nodes.Add(node);
            if (!Application.isPlaying)
            {
                AssetDatabase.AddObjectToAsset(node, this);//关联父资产
            }
            Undo.RegisterCreatedObjectUndo(node, "Behaviour Tree(Create Node)");
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            return node;
        }

        public void DeleteNode(Node.Node node)
        {
            Undo.RecordObject(this, "Behaviour Tree(Delete Node)");
            nodes.Remove(node);
            if(rootNode==node) rootNode = null;
            Undo.DestroyObjectImmediate(node);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        //给节点添加子节点 并不会直接影响树的nodes
        public bool AddChildNode(Node.Node parent,Node.Node child)
        {
            Undo.RecordObject(this, "Behaviour Tree(Add Child Node)");
            var result = parent.AddChildNode(child);
            EditorUtility.SetDirty(this);
            return result;
        }

        public bool RemoveChildNode(Node.Node parent, Node.Node child)
        {
            Undo.RecordObject(this, "Behaviour Tree(Remove Child Node)");
            var result = parent.RemoveChildNode(child);
            EditorUtility.SetDirty(this);
            return result;
        }
#endif

        public List<Node.Node> GetChildNodes(Node.Node parent)
        {
            var childNodes = new List<Node.Node>();
            switch (parent)
            {
                case RootNode rootNode:
                    if (rootNode.child)
                    {
                        childNodes.Add(rootNode.child);
                    }
                    return childNodes;
                case DecoratorNode decoratorNode:
                    if (decoratorNode.child)
                    {
                        childNodes.Add(decoratorNode.child);
                    }
                    return childNodes;
                case CompositeNode compositeNode:
                    return compositeNode.children.Where(child=>child!=null).ToList();
                default: 
                    return childNodes;
            }
        }

        public virtual TreeState Tick(float deltaTime,object payload)
        {
            Time += deltaTime;
            treeState = rootNode.Tick(deltaTime, payload) switch
            {
                NodeState.Success => TreeState.Success,
                NodeState.Failure => TreeState.Failure,
                NodeState.Running => TreeState.Running,
                _ => TreeState.Running,
            };
            return treeState;
        }

        public BehaviourTree Clone()
        {
            var behaviourTree = Instantiate(this);
            behaviourTree.blackboard = Instantiate(blackboard);
            behaviourTree.rootNode = rootNode.Clone() as RootNode;
            var cloneNodes = new List<Node.Node>();
            Dfs(behaviourTree.rootNode, (child) =>
            {
                child.Tree = behaviourTree;
                child.blackboard = behaviourTree.blackboard;
                cloneNodes.Add(child);
            });
            return behaviourTree;
        }
        public void Destroy()
        {
            GameObject.Destroy(this);
            GameObject.Destroy(blackboard);
            nodes.ForEach(GameObject.Destroy);
            nodes.Clear();
        }

        public void Dfs(Node.Node node,Action<Node.Node> visitor)
        {
            visitor.Invoke(node);
            GetChildNodes(node).ForEach(child => Dfs(child, visitor));
        }
    }
}

