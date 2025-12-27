using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Framework.Common.BehaviourTree
{
    public enum TreeState
    {
        Initialized,
        Running,
        Success,
        Failure,
    }

    [CreateAssetMenu(menuName ="Behaviour Tree")]
    public class BehaviourTree : ScriptableObject
    {
        public float Time;
    }
}

