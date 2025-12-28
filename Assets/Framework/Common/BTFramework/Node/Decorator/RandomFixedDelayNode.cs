using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Random Fixed Delay")]
    public class RandomFixedDelayNode : DecoratorNode
    {
        [SerializeField] private float minDelayTime = 0f;
        [SerializeField] private float maxDelayTime = 1f;
        [SerializeField] private bool recordTimeAfterAbort = false;
        public override string Description => "延迟随机固定时间执行节点，不受时间缩放影响";
        private float _startTime;
        private float _duration;
        protected override void OnStart(object payload)
        {
            _startTime=Time.unscaledTime;
            _duration=Random.Range(minDelayTime, maxDelayTime);
        }

        protected override void OnAbort(object payload)
        {
            base.OnAbort(payload);
            _duration -= Time.unscaledTime - _startTime;
            if(recordTimeAfterAbort ) _startTime =Time.unscaledTime;
        }
        protected override void OnResume(object payload)
        {
            if(recordTimeAfterAbort) _duration-=Time.unscaledTime - _startTime;
            _startTime = Time.unscaledTime;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            if(Time.unscaledTime - _startTime >= deltaTime) return child.Tick(deltaTime, payload);
            return NodeState.Running;
        }
    }
}