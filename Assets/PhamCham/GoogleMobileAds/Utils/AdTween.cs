using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GoogleMobileAds.Common;
using UnityEngine;

public class AdTween {
    public static void DelayCallTween(float delay, Action action) {
        DOVirtual.DelayedCall(delay, () => {
            try {
                action?.Invoke();
            }
            catch (Exception ex) {
                Debug.LogException(ex);
            }
        }).Play();
    }

    public static void ExecuteSafeInUpdate(Action action, float delay = 0.1f) {
        MobileAdsEventExecutor.ExecuteInUpdate(() => {
            DelayCallTween(delay, action);
        });
    }
}
