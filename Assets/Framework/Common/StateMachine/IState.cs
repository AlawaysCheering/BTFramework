using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.StateMachine
{
    //分层有限状态机的状态接口
    public interface IState
    {
        IStateMachine Parent { set; get; }
        bool SetDefault { get; }
        string Name { get;}

        void Init();
        bool AllowEnter([CanBeNull] IState currentState);
        void Enter([CanBeNull] IState previousState);
        void RenderTick(float deltaTime);
        void LogicTick(float fixedDeltaTime);
        void Exit([CanBeNull] IState currentState);
        void Clear();
    }
}

