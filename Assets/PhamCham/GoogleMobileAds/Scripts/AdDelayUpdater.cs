using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PhamCham.GoogleMobileAds {
    public class AdDelayUpdater : MonoBehaviour {
        private float delaySeconds = 0;

        public bool IsDelaying() {
            return delaySeconds > 0;
        }

        public void DelaySomeSeconds() {
            delaySeconds = 12;
        }

        private void Update() {
            if (delaySeconds > 0) {
                delaySeconds -= Time.deltaTime * Time.timeScale;
            }
        }
    }
}