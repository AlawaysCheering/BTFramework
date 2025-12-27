using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Time Block")]
    public class TimeBlockNode : DecoratorNode
    {
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool recordTimeAfterAbort = false;

        private float _abortTime;
        private float _time;
        private float _duration;

        public override string Description => "时间阻塞节点，受时间缩放影响，在一段时间内执行子节点返回运行中，时间完毕后返回成功";

        protected override void OnStart(object payload)
        {
            _time = 0f;
            _duration = duration;
        }

        protected override void OnAbort(object payload)
        {
            base.OnAbort(payload);
            if (recordTimeAfterAbort)
            {
                _abortTime = Tree.Time;
            }
        }

        protected override void OnResume(object payload)
        {
            base.OnResume(payload);
            if (recordTimeAfterAbort)
            {
                _time+= Tree.Time-_abortTime;
            }
        }
        protected override NodeState OnTick(float deltaTime, object payload)
        {
            _time+= deltaTime;
            if (_time >= _duration) return NodeState.Success;
            child.Tick(deltaTime, payload);
            return NodeState.Running;
        }
    }
}