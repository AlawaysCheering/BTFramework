using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.BehaviourTree
{
    public class BehaviourTreeComponent : MonoBehaviour
    {
        [SerializeField] private BehaviourTreeExecutor behaviourTreeExecutor;

        private void Awake()
        {
            behaviourTreeExecutor.Init();
            behaviourTreeExecutor.Tick(0);
        }
        private void Update()
        {
            behaviourTreeExecutor.Tick(Time.deltaTime);
        }
        private void OnDestroy()
        {
            behaviourTreeExecutor.Destory();
        }
    }
}

