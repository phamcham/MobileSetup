using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;
using UnityEngine.Events;

namespace PhamCham.GoogleMobileAds {
    public class OpenAdUnit : AdUnit {
#pragma warning disable 414 // disable adUnitId not use in other platform
#if UNITY_ANDROID
        private const string adUnitTestId = "ca-app-pub-3940256099942544/3419835294";
#else
        private const string adUnitTestId = "ca-app-pub-3940256099942544/5662855259";
#endif
        // khoang cach giua cac lan show quang cao
        // luu y: request van giu nguyen, chi kiem tra thoi gian show giua cac quang cao
        [Header("Interval")]
        [SerializeField] protected float intervalBetweenAds = 20f;
        [SerializeField] float intervalHangout = 20;
        [SerializeField] protected float delayFirstRequestCall = 20f;

        // [Header("Events")]
        // [SerializeField] private UnityEvent<bool> OnLoadingChange;
        // [SerializeField] private UnityEvent<bool> OnSoundTempChange;

        private readonly TimeSpan APPOPEN_TIMEOUT = TimeSpan.FromHours(4);
        private DateTime appOpenExpireTime;

        private AppOpenAd appOpenAd;
        private bool isShowingAd = false;
        // private DateTime loadTime;
        private DateTime hangoutTime;

        public float IntervalBetweenAds {
            get { return intervalBetweenAds; }
            set { intervalBetweenAds = value; }
        }

        public float IntervalHangout {
            get { return intervalHangout; }
            set { intervalHangout = value; }
        }

        public float DelayFirstRequestCall {
            get { return delayFirstRequestCall; }
            set { delayFirstRequestCall = value; }
        }

        /// <summary>
        /// tam thoi khong can phai xem quang cao mo app 1 lan
        /// </summary>
        //private bool ignoreOnNextCall;

        //public void IgnoreAdOnNextCallWhenCloseOther() {
        //    ignoreOnNextCall = true;
        //    // 1s sau khi load hien quang cao nen de tranh loi thi 3s sau se khoi phuc ignore
        //    AdTween.ExecuteSafeInUpdate(() => ignoreOnNextCall = false, 3f);
        //}

        public override void Initialize() {
            if (AdUtils.IsRemoveAds()) {
                return;
            }

            AdTween.DelayCallTween(delayFirstRequestCall, LoadAd);

            hangoutTime = DateTime.Now;
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
        }

        private void OnAppStateChanged(AppState state) {
            // Display the app open ad when the app is foregrounded.
            if (state == AppState.Foreground) {
                if (IsAdAvailable()) {
                    AdTween.ExecuteSafeInUpdate(() => {
                        // OnLoadingChange?.Invoke(true);
                        AdUtils.OnLoadingChange(true);
                        // OnSoundTempChange?.Invoke(false);
                        AdUtils.OnSoundTempChange(false);

                        AdTween.DelayCallTween(1, ShowAdIfAvailable);
                    });
                }
            }

            if (state == AppState.Background) {
                hangoutTime = DateTime.Now;
            }
        }

        DateTime intervalBetweenAdsTime = DateTime.Now.AddHours(-1);
        bool CheckInterval() {
            return (DateTime.Now - intervalBetweenAdsTime).TotalSeconds >= intervalBetweenAds;
        }

        public void LoadAd() {
            if (AdUtils.IsRemoveAds()) {
                return;
            }

#if UNITY_ANDROID
            string adUnitId = adUnitId_Android.Trim();
#else
            string adUnitId = adUnitId_IOS.Trim();
#endif
            if (isTesting)
                adUnitId = adUnitTestId.Trim();

            AdRequest request = new AdRequest.Builder().Build();

            // Load an app open ad for portrait orientation
            AppOpenAd.Load(adUnitId, ScreenOrientation.Portrait, request, (AppOpenAd ad, LoadAdError loadError) => {
                if (loadError != null) {
                    Debugger.Log(this, () => "App open ad failed to load with error: " + loadError.GetMessage());
                    AdTween.ExecuteSafeInUpdate(LoadAd, 20);
                    return;
                }
                else if (ad == null) {
                    Debugger.Log(this, () => "App open ad failed to load.");
                    AdTween.ExecuteSafeInUpdate(LoadAd, 20);
                    return;
                }

                Debugger.Log(this, () => "App Open ad loaded. Please background the app and return.");
                appOpenAd = ad;
                appOpenExpireTime = DateTime.Now + APPOPEN_TIMEOUT;

                ad.OnAdFullScreenContentOpened += () => {
                    Debugger.Log(this, () => "App open ad opened.");
                    HandleAdDidPresentFullScreenContent();
                };
                ad.OnAdFullScreenContentClosed += () => {
                    Debugger.Log(this, () => "App open ad closed.");
                    HandleAdDidDismissFullScreenContent();
                };
                ad.OnAdImpressionRecorded += () => {
                    Debugger.Log(this, () => "App open ad recorded an impression.");
                };
                ad.OnAdClicked += () => {
                    Debugger.Log(this, () => "App open ad recorded a click.");
                };
                ad.OnAdFullScreenContentFailed += (AdError error) => {
                    HandleAdFailedToPresentFullScreenContent();
                    Debugger.LogWarning(this, () => "App open ad failed to show with error: " + error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) => {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "App open ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    Debug.Log(msg);
                };
            });
        }

        public void ShowAdIfAvailable() {
            if (!IsAdAvailable() || isShowingAd) {
                Debugger.LogWarning(this, () => "Open Ad can't show! IsAdAvailable(): " + IsAdAvailable() + ", isShowingAd: " + isShowingAd);
                ContinueFlow();
                return;
            }

            appOpenAd.Show();
        }

        public bool IsAdAvailable() {
            bool removeAd = AdUtils.IsRemoveAds();
            if (removeAd) {
                Debugger.LogWarning(this, () => "[OpenAd] no show cause removed ads");
                return false;
            }

            bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
            if (!hasInternet) {
                Debugger.LogWarning(this, () => "[OpenAd] unavailable cause no internet");
                return false;
            }

            bool appOpenLoaded = appOpenAd != null;
            if (!appOpenLoaded) {
                Debugger.LogWarning(this, () => "[OpenAd] unavailable cause ad unloaded");
                return false;
            }

            bool adNonExpire = DateTime.Now < appOpenExpireTime;
            if (!adNonExpire) {
                Debugger.LogWarning(this, () => "[OpenAd] unavailable cause ad hold more than 4 hours");
                return false;
            }

            bool hangoutEnough = (DateTime.Now - hangoutTime).TotalSeconds >= intervalHangout;
            if (!hangoutEnough) {
                Debugger.LogWarning(this, () => "[OpenAd] unavailable cause hangout too fast");
                return false;
            }

            bool intervalBetweenAdsEnough = CheckInterval();
            if (!intervalBetweenAdsEnough) {
                Debugger.LogWarning(this, () => "[OpenAd] unavailable cause has delay between open ads");
                return false;
            }

            bool hasDelayAds = GoogleMobileAdsManager.AdDelayer.IsDelaying();
            if (hasDelayAds) {
                Debugger.LogWarning(this, () => "[OpenAd] unavailable cause has delay between all types ads");
                return false;
            }

            return true;
        }

        private void HandleAdDidDismissFullScreenContent() {
            AdTween.ExecuteSafeInUpdate(() => {
                // Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
                isShowingAd = false;
                appOpenAd = null;
                ContinueFlow();
                intervalBetweenAdsTime = DateTime.Now;

                // load ad again
                GoogleMobileAdsManager.AdDelayer.DelaySomeSeconds();
                AdTween.DelayCallTween(5, LoadAd);
            });
        }

        private void HandleAdFailedToPresentFullScreenContent() {
            AdTween.ExecuteSafeInUpdate(() => {
                // Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
                appOpenAd = null;
                ContinueFlow();
                AdTween.DelayCallTween(20, LoadAd);
            });
        }

        private void ContinueFlow() {
            // OnLoadingChange?.Invoke(false);
            AdUtils.OnLoadingChange(false);
            // OnSoundTempChange?.Invoke(true);
            AdUtils.OnSoundTempChange(true);
        }

        private void HandleAdDidPresentFullScreenContent() {
            AdTween.ExecuteSafeInUpdate(() => {
                GoogleMobileAdsManager.AdDelayer.DelaySomeSeconds();
                isShowingAd = true;
            });
        }
    }
}