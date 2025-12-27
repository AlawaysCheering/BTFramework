using Framework.Core.Attribute;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Framework.Common.BehaviourTree.Node.Decorator
{
    public class TimeDelayNode : DecoratorNode
    {
        [SerializeField] private float delayTime = 1f;
        [SerializeField] private bool recordTimeAfterAbort = false;

        private float _abortTime;
        private float _time;
        private float _duration;

        public override string Description => "时间延迟节点，受时间缩放影响";

        protected override void OnStart(object payload)
        {
            _time = 0f;
            _duration = delayTime;
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
            if (recordTimeAfterAbort)
            {
                _time += Tree.Time - _abortTime;//把被打断到复苏的时间也加上
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            _time += deltaTime;
            if (_time >= _duration)
            {
                return child.Tick(deltaTime, payload);
            }
            return NodeState.Running;
        }


    }
}


