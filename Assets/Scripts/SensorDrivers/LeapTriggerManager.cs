using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.SensorDrivers
{
    public class LeapTriggerManager
    {
        private class TriggerPackage
        {
            public IEnumerable<Transform> Transforms;
            public Predicate<IEnumerable<Transform>> Trigger;
            public Action Consequence;
        }

        private List<TriggerPackage> _triggerStore; 

        private LeapTriggerManager()
        {
            _triggerStore = new List<TriggerPackage>();
            
        }

        public void RegisterTrigger(IEnumerable<Transform> inputTransforms, 
            Predicate<IEnumerable<Transform>> triggerPredicate,
            Action consequeceAction)
        {
            var newTrigger = new TriggerPackage();

            newTrigger.Trigger = triggerPredicate;
            newTrigger.Transforms = inputTransforms;
            newTrigger.Consequence = consequeceAction;

            _triggerStore.Add(newTrigger);
        }

        public void Poll()
        {
            foreach (var trigger in _triggerStore)
            {
                if (trigger.Trigger(trigger.Transforms))
                    trigger.Consequence.Invoke();
            }
        }


    }
}
