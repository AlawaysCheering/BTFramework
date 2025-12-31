using Framework.Common.Debug;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Framework.Common.StateMachine
{
    public class State : MonoBehaviour,IState
    {
        public IStateMachine Parent { get; set; }

        [SerializeField] private bool setDefault;
        
        public bool SetDefault =>setDefault;

        protected float DeltaTime { private set; get; }
        protected float FixedDeltaTime { private set; get; }

        private string _name;

        public string Name
        {
            get
            {
                if(_name != null) return _name;
                if (!gameObject.IsDestroyed())
                {
                    _name = gameObject.name;
                    return _name;
                }
                _name=GetType().Name;
                return _name;
            }
        }
        public void Init()
        {
            OnInit();
        }

        public virtual bool AllowEnter(IState currentState) => true;

        public void Enter(IState previousState)
        {
            OnEnter(previousState);
        }

        public void RenderTick(float deltaTime)
        {
            DeltaTime = deltaTime;
            OnRenderTick(deltaTime);
        }

        public void LogicTick(float fixedDeltaTime)
        {
            FixedDeltaTime = fixedDeltaTime;
            OnLogicTick(fixedDeltaTime);
        }

        public void Exit(IState nextState)
        {
            OnExit(nextState);
        }

        public void Clear()
        {
            OnClear();
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnEnter(IState previousState)
        {
            DebugUtil.LogYellow($"{Name}×´Ì¬¼¤»î");
        }

        protected virtual void OnRenderTick(float deltaTime)
        {
        }

        protected virtual void OnLogicTick(float fixedDeltaTime)
        {
        }

        protected virtual void OnExit(IState nextState)
        {
            DebugUtil.LogYellow($"{Name}×´Ì¬Ê§»î");
        }
        protected virtual void OnClear()
        {
        }
    }
}
