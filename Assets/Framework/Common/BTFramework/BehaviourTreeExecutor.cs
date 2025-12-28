using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.BehaviourTree
{
    public class BehaviourTreeExecutor
    {
        [SerializeField] private BehaviourTree treeTemplate;//Ô¤ÉèÊ÷

        private BehaviourTree _runtimeTree;
#if UNITY_EDITOR
        public BehaviourTree RuntimeTree => _runtimeTree;
#endif

        public virtual void Init()
        {
            _runtimeTree = treeTemplate.Clone();
        }

        public virtual void Destory()
        {
            if (_runtimeTree)
            {
                _runtimeTree.Destroy();
                _runtimeTree = null;    
            }
        }

        public void UseBlackboard(Action<Blackboard.Blackboard> callback)
        {
            if (_runtimeTree)
            {
                callback.Invoke(_runtimeTree.blackboard);
            }
        }

        public void Dfs(Action<Node.Node> visitor)
        {
            if (_runtimeTree)
            {
                _runtimeTree.Dfs(_runtimeTree.rootNode, visitor);
            }
        }

        public virtual TreeState Tick(float deltaTime,object payload = null)
        {
            if (!_runtimeTree) return TreeState.Failure;
            return _runtimeTree.Tick(_runtimeTree.treeState==TreeState.Running ? deltaTime : 0f, payload);
        }

        public TreeState GetTreeState()
        {
            return _runtimeTree?.treeState ?? TreeState.Failure;
        }
    }

}
