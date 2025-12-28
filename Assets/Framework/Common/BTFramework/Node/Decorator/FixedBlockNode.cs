using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Fixed Block")]
    public class FixedBlockNode : DecoratorNode
    {
        [SerializeField] private float duration;
        [SerializeField] private bool recordTimeAfterAbort = false;

        private float _startTime;
        private float _duration;
        protected override void OnStart(object payload)
        {
            _startTime = Time.unscaledTime;
            _duration = duration;
        }
        public override string Description => "阻塞固定时间一直执行节点，不受时间缩放影响（在一段固定时间内执行子节点返回运行中，时间完毕后返回成功）";
        protected override void OnAbort(object payload)
        {
            base.OnAbort(payload);
            duration-= Time.unscaledTime-_startTime;
            if(recordTimeAfterAbort )
            {
                _startTime = Time.unscaledTime;
            }
        }
        protected override void OnResume(object payload)
        {
            if(recordTimeAfterAbort )
            {
                duration -= Time.unscaledTime-_startTime;
            }
            _startTime = Time.unscaledTime;
        }
        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if(Time.unscaledTime-_startTime < duration)
            {
                child?.Tick(deltaTime, payload);
                return NodeState.Running;
            }
            child?.Abort(payload);
            return NodeState.Success;
        }
    }

}
