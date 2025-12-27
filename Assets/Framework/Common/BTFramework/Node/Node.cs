using Framework.Common.Debug;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Framework.Common.BehaviourTree.Node
{
    public class Node : ScriptableObject, INode
    {
        [HideInInspector]
        public string guid;

        [HideInInspector]
        public Vector2 position;

        [HideInInspector]
        public int executeOrder = 1;

        [TextArea] public string comment;
        [HideInInspector] public Blackboard.Blackboard blackboard;
        [SerializeField] public bool stopWhenAbort = true;
        [SerializeField] public bool debugLifecycle;
        [FormerlySerializedAs("debug")][SerializeField] public bool debugTime;

        [NonSerialized] public BehaviourTree Tree;

        private bool _started = false;
        private bool _aborted = false;
        private NodeState _state = NodeState.Running;

        public bool Started => _started;

        public bool Aborted => _aborted;
        public NodeState State => _state;
        public bool StopWhenAbort => stopWhenAbort;
        public virtual string Description => "节点";

        private string Tag
        {
            get
            {
                if (string.IsNullOrEmpty(comment))
                {
#if UNITY_EDITOR
                    return $"{GetType().Name}.{guid}";
#else
                    return $"{GetType().Name}";
#endif
                }
                return $"{GetType().Name}.{comment}";
            }
        }

        public void Reset(object payload)
        {
            _started = false;
            if (debugLifecycle)
            {
                DebugUtil.LogCyan($"Node {Tag} Reset");
            }
        }

        public NodeState Tick(float deltaTime, object payload)
        {
            var startTime = Time.realtimeSinceStartup;
            var tickDeltaTime = deltaTime;
            if (_aborted)
            {
                _aborted = false;
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Resume");
                }
                tickDeltaTime = 0f;
                OnResume(payload);
            }

            if (!_started)
            {
                _started = true;
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Start");
                }

                tickDeltaTime = 0f;
                OnStart(payload);
            }

            if (debugLifecycle)
            {
                DebugUtil.LogCyan($"Node {Tag} Tick");
            }

            _state = OnTick(tickDeltaTime, payload);

            if (_state == NodeState.Failure || _state == NodeState.Success)
            {
                _started = false;
                _aborted = false;
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Stop");
                }
                OnStop(payload);
            }

            var endTime = Time.realtimeSinceStartup;
            if (debugTime)
            {
                DebugUtil.LogGrey($"Node {Tag} TickTime: {(endTime - startTime) * 1000}ms");
            }
            return _state;
        }

        public void Abort(object payload)
        {
            if (!_started)
            {
                return;
            }
            if (_aborted)
            {
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Already Aborted");
                }
                return;
            }
            _aborted = true;
            if (debugLifecycle)
            {
                DebugUtil.LogCyan($"Node {Tag} Abort");
            }
            OnAbort(payload);

            if (stopWhenAbort)
            {
                _started = false;
                _aborted = false;
                _state=NodeState.Failure;
                if (debugLifecycle)
                {
                    DebugUtil.LogCyan($"Node {Tag} Stop");
                }
                OnStop(payload);
            }
        }

        //节点启动时执行的函数
        protected virtual void OnStart(object payload)
        {
        }
        //节点结束时执行的函数
        protected virtual void OnStop(object payload)
        {
        }

        //节点被打断时执行的函数
        protected virtual void OnAbort(object payload)
        {
        }
        //节点在被打断后再次调用Tick会执行这个函数
        protected virtual void OnResume(object payload)
        {
        }

        //节点Tick时执行的函数
        protected virtual NodeState OnTick(float deltaTime, object payload) 
        {
            return NodeState.Failure;
        }

        //克隆，用于多个组件同时执行同一文件的行为树时，克隆数据
        public virtual Node Clone()
        {
            return Instantiate(this);
        }
#if UNITY_EDITOR
        //用于编辑器 新增子节点
        public virtual bool AddChildNode(Node child)
        {
            return false;
        }

        //用于编辑器 删除子节点
        public virtual bool RemoveChildNode(Node child)
        {
            return false;
        }
#endif

    }

}

