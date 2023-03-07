using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace PhamCham.GoogleMobileAds {
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AdDelayUpdater))]
    public class GoogleMobileAdsManager : MonoBehaviour {
        [SerializeField] private List<string> testDevicesIOS;
        [SerializeField] private List<string> testDevicesAndroid;
        [SerializeField] private TagForChildDirectedTreatment tagForChild = TagForChildDirectedTreatment.Unspecified;
        [SerializeField] private bool dontDestroyOnLoad = true;

        private static GoogleMobileAdsManager _instance = null;
        private static GoogleMobileAdsManager Instance {
            get {
                if (_instance == null) {
                    _instance = FindObjectOfType<GoogleMobileAdsManager>(true);
                }
                return _instance;
            }
        }

        private bool isInitialized = false;

        private void Awake() {
            if (_instance == null) {
                _instance = this;
            }

            Initialize();

            if (dontDestroyOnLoad) {
                DontDestroyOnLoad(this);
            }
        }

        private void Initialize() {
            if (isInitialized)
                return;
            isInitialized = true;

            MobileAds.SetiOSAppPauseOnBackground(true);

            List<string> deviceIds = new List<string>() { AdRequest.TestDeviceSimulator };

            // Add some test device IDs (replace with your own device IDs).
#if UNITY_IOS
            if (testDevicesIOS != null)
                deviceIds.AddRange(testDevicesIOS);
#elif UNITY_ANDROID
            if (testDevicesAndroid != null)
                deviceIds.AddRange(testDevicesAndroid);

#endif

            // Configure TagForChildDirectedTreatment and test device IDs.
            RequestConfiguration requestConfiguration =
                new RequestConfiguration.Builder()
                .SetTagForChildDirectedTreatment(tagForChild)
                .SetTestDeviceIds(deviceIds)
                .build();

            MobileAds.SetRequestConfiguration(requestConfiguration);

            // Initialize the Google Mobile Ads SDK.
            MobileAds.Initialize(initstatus => {
                // Callbacks from GoogleMobileAds are not guaranteed to be called on
                // main thread.
                // In this example we use MobileAdsEventExecutor to schedule these calls on
                // the next Update() loop.
                MobileAdsEventExecutor.ExecuteInUpdate(() => {
                    AdUnit[] adUnits = GetComponents<AdUnit>();
                    foreach (AdUnit adUnit in adUnits) {
                        adUnit.Initialize();
                    }
                });
            });
        }

        public static T GetAdUnit<T>() where T : AdUnit {
            if (Instance == null) return null;

            Instance.TryGetComponent(out T adUnit);
            if (adUnit == null) {
                Debug.LogError(nameof(T) + " have not been added yet!");
            }
            return adUnit;
        }

        public static AdDelayUpdater AdDelayer => Instance.GetComponent<AdDelayUpdater>();
    }
}