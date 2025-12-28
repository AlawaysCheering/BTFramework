using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Random Time Block")]
    public class RandomTimeBlockNode : DecoratorNode
    {
        [SerializeField] private float minDelayTime = 0f;
        [SerializeField] private float maxDelayTime = 1f;
        [SerializeField] private bool recordTimeAfterAbort = false;

        private float _abortTime;
        private float _time;
        private float _duration;

        public override string Description => "阻塞随机时间执行节点，受时间缩放影响(一段时间内一直执行子节点返回运行中，时间完毕后返回运行成功)";
        protected override void OnStart(object payload)
        {
            _time = 0f;
            _duration = Random.Range(minDelayTime, maxDelayTime);
        }
        protected override void OnAbort(object payload)
        {
            base.OnAbort(payload);
            if (recordTimeAfterAbort)
                _abortTime = Tree.Time;
        }
        protected override void OnResume(object payload)
        {
            if (recordTimeAfterAbort) _time += Tree.Time - _abortTime;
        }
        protected override NodeState OnTick(float deltaTime, object payload)
        {
            _time += deltaTime;
            if (_time >= _duration)
            {
                child.Abort(payload);
                return NodeState.Success;
            }
            child.Tick(deltaTime, payload);
            return NodeState.Running;
        }
    }
}