using UnityEngine;
using UnityEngine.Rendering;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Fixed Delay")]
    public class FixedDelayNode : DecoratorNode
    {
        [SerializeField] private float delayTime = 1f;
        [SerializeField] private bool recordTimeAfterAbort = false;
        public override string Description => "延迟固定时间执行节点，不受时间缩放影响";
        private float _startTime;
        private float _duration;

        protected override void OnAbort(object payload)
        {
            base.OnAbort(payload);
            _duration -=Time.unscaledTime - _startTime;
            if (recordTimeAfterAbort)
            {
                _startTime = Time.unscaledTime;
            }
        }
        protected override void OnResume(object payload)
        {
            if(recordTimeAfterAbort)
            {
                _duration -=Time.unscaledTime - _startTime;
            }
            _startTime = Time.unscaledTime;
        }
        protected override void OnStart(object payload)
        {
            _startTime = Time.unscaledTime;
            _duration = delayTime;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if (Time.unscaledTime - _startTime >= _duration)
            {
                return child.Tick(deltaTime, payload);
            }
            return NodeState.Running;
        }
    }
}
