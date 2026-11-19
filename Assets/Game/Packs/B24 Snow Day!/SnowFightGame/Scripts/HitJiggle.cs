using System;
using System.Collections;
using UnityEngine;

namespace SnowDay.SnowFight {
    public class HitJiggle : MonoBehaviour {
        public AnimationCurve jiggleCurve;
        private Vector3 originalScale;
        
        private void Start() {
            originalScale = transform.localScale;
        }

        public void Jiggle() {
            StartCoroutine(JiggleCoroutine());
        }

        private IEnumerator JiggleCoroutine() {
            float timer = 0;
            float curveDuration = jiggleCurve.keys[jiggleCurve.length - 1].time;
            
            while(timer < curveDuration) {
                timer += Time.deltaTime;
                float curveValue = jiggleCurve.Evaluate(timer);
                
                transform.localScale = originalScale * curveValue;
                
                yield return null;
            }
        }
    }
}