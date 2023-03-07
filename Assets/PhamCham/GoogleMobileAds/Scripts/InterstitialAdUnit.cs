using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.Events;

namespace PhamCham.GoogleMobileAds {
    public class InterstitialAdUnit : AdUnit {
#pragma warning disable 414 // disable adUnitId not use in other platform
#if UNITY_ANDROID
        private const string adUnitTestId = "ca-app-pub-3940256099942544/1033173712";
#else
        private const string adUnitTestId = "ca-app-pub-3940256099942544/4411468910";
#endif
        // khoang cach giua cac lan show quang cao
        // luu y: request van giu nguyen, chi kiem tra thoi gian show giua cac quang cao
        [Header("Interval")]
        [SerializeField] protected float intervalBetweenAds = 20f;
        [SerializeField] protected float delayFirstRequestCall = 30f;

        //[Header("Events")]
        //[SerializeField] private UnityEvent<bool> OnLoadingChange;
        //[SerializeField] private UnityEvent<bool> OnSoundTempChange;

        private InterstitialAd interstitialAd;
        //OpenAdUnit OpenAd => GoogleMobileAdsManager.GetAdUnit<OpenAdUnit>();

        public float IntervalBetweenAds {
            get { return intervalBetweenAds; }
            set { intervalBetweenAds = value; }
        }

        public float DelayFirstRequestCall {
            get { return delayFirstRequestCall; }
            set { delayFirstRequestCall = value; }
        }

        /// <summary>
        /// Thực thi sau khi xem xong quảng cáo. True: xem qcao, False: không xem qcao
        /// </summary>
        private Action<bool> OnContinue;

        /// <summary>
        /// Một Request đã nhận được phản hồi hay chưa
        /// </summary>
        private bool isRequestedAndWaitForResponing = false;

        /// <summary>
        /// Có một quảng cáo sẵn sàng để xem, không cần request lại lúc này
        /// </summary>
        private bool isReadyToShow = false;

        /// <summary>
        /// Số quảng cáo lỗi liên tiếp, nếu nhiều quá thì coi chừng có j không ổn
        /// </summary>
        private int requestFailedContinousCount = 0;

        /// <summary>
        /// Hàm khởi tạo, luôn gọi hàm này đầu tiên
        /// </summary>
        public override void Initialize() {
            if (AdUtils.IsRemoveAds()) {
                return;
            }

            AdTween.DelayCallTween(delayFirstRequestCall, RequestAndLoad);
        }

        private void RequestAndLoad() {
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

            // Clean up before using it
            if (interstitialAd != null) {
                interstitialAd.Destroy();
            }

            AdRequest request = new AdRequest.Builder().Build();
            InterstitialAd.Load(adUnitId, request, (InterstitialAd ad, LoadAdError loadError) => {
                if (loadError != null) {
                    Debugger.Log(this, () => "[Interstitial ad] failed to load with error: " + loadError.GetMessage());
                    HandleAdFailedToLoadEvent();
                    return;
                }
                else if (ad == null) {
                    Debugger.Log(this, () => "[Interstitial ad] failed to load.");
                    HandleAdFailedToLoadEvent();
                    return;
                }

                Debugger.Log(this, () => "[Interstitial ad] loaded.");
                interstitialAd = ad;
                HandleAdLoadedEvent();

                ad.OnAdFullScreenContentOpened += () => {
                    Debugger.Log(this, () => "[Interstitial ad] opening.");
                    HandleAdOpeningEvent();
                };
                ad.OnAdFullScreenContentClosed += () => {
                    Debugger.Log(this, () => "[Interstitial ad] closed.");
                    HandleAdClosedEvent(true);
                };
                ad.OnAdImpressionRecorded += () => {
                    Debugger.Log(this, () => "[Interstitial ad] recorded an impression.");
                };
                ad.OnAdClicked += () => {
                    Debugger.Log(this, () => "[Interstitial ad] recorded a click.");
                };
                ad.OnAdFullScreenContentFailed += (AdError error) => {
                    HandleAdFailedToShowEvent();
                    Debugger.Log(this, () => "[Interstitial ad] failed to show with error: " + error.GetMessage());
                };
                ad.OnAdPaid += (AdValue adValue) => {
                    string msg = string.Format("{0} (currency: {1}, value: {2}",
                                               "[Interstitial ad] received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    Debugger.Log(this, () => msg);
                };
            });

            isRequestedAndWaitForResponing = true;
        }

        private void HandleAdClosedEvent(bool success) {
            AdTween.ExecuteSafeInUpdate(() => {
                isRequestedAndWaitForResponing = false;
                isReadyToShow = false;

                if (success) {
                    // khac null co nghia la ham dc goi tu he thong, khong thi la cua minh
                    requestFailedContinousCount = 0;

                    // delay ad and reset time between adses
                    GoogleMobileAdsManager.AdDelayer.DelaySomeSeconds();
                    intervalBetweenAdsTime = DateTime.Now;

                    // request again when ads close
                    StartCoroutine(RequestAfterDelay(5));
                }

                // OnSoundTempChange?.Invoke(true);
                AdUtils.OnSoundTempChange(true);
                // OnLoadingChange?.Invoke(false);
                AdUtils.OnLoadingChange(false);

                ExecuteContinue(success);
            });
        }

        private void HandleAdOpeningEvent() {
            isRequestedAndWaitForResponing = false;
            isReadyToShow = false;
            AdTween.ExecuteSafeInUpdate(() => {
                // OnSoundTempChange?.Invoke(false);
                AdUtils.OnSoundTempChange(false);
            });
        }

        private void HandleAdLoadedEvent() {
            isRequestedAndWaitForResponing = false;
            isReadyToShow = true;
        }

        private void HandleAdFailedToShowEvent() {
            AdTween.ExecuteSafeInUpdate(() => {
                isRequestedAndWaitForResponing = false;
                isReadyToShow = false;

                requestFailedContinousCount = Mathf.Min(10, requestFailedContinousCount + 1);
                // OnSoundTempChange?.Invoke(true);
                AdUtils.OnSoundTempChange(true);
                // OnLoadingChange?.Invoke(false);
                AdUtils.OnLoadingChange(false);

                ExecuteContinue(false);
                RequestWhenAdFailed();
            });
        }

        private void HandleAdFailedToLoadEvent() {
            AdTween.ExecuteSafeInUpdate(() => {
                isRequestedAndWaitForResponing = false;
                isReadyToShow = false;

                requestFailedContinousCount = Mathf.Min(10, requestFailedContinousCount + 1);

                RequestWhenAdFailed();
            });
        }

        public bool IsAdAvailable() {
            bool removeAd = AdUtils.IsRemoveAds();
            if (removeAd) {
                Debugger.LogWarning(this, () => "[InterstitialAd] no show cause removed ads");
                return false;
            }

            bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
            if (!hasInternet) {
                Debugger.LogWarning(this, () => "[InterstitialAd] unavailable cause no internet");
                return false;
            }

            bool intersititalLoaded = interstitialAd != null && interstitialAd.CanShowAd();
            if (!intersititalLoaded) {
                Debugger.LogWarning(this, () => "[InterstitialAd] unavailable cause ad unloaded");
                return false;
            }

            bool intervalBetweenAdsEnough = CheckInterval();
            if (!intervalBetweenAdsEnough) {
                Debugger.LogWarning(this, () => "[InterstitialAd] unavailable cause has delay between interstitial ads");
                return false;
            }

            bool hasDelayAds = GoogleMobileAdsManager.AdDelayer.IsDelaying();
            if (hasDelayAds) {
                Debugger.LogWarning(this, () => "[InterstitialAd] unavailable cause has delay between all types ads");
                return false;
            }

            return true;
        }

        public void ShowAd(Action<bool> onContinue) {
            OnContinue = onContinue;

            if (IsAdAvailable()) {
                // OnLoadingChange?.Invoke(true);
                AdUtils.OnLoadingChange(true);
                GoogleMobileAdsManager.AdDelayer.DelaySomeSeconds();
                AdTween.DelayCallTween(1.5f, () => {
                    interstitialAd.Show();
                });
            }
            else {
                //RequestAndLoad();
                HandleAdClosedEvent(false);
                //ExecuteContinue();
            }
        }

        DateTime intervalBetweenAdsTime = DateTime.Now.AddHours(-1);
        bool CheckInterval() {
            return (DateTime.Now - intervalBetweenAdsTime).TotalSeconds >= intervalBetweenAds;
        }

        private void ExecuteContinue(bool success) {
            if (OnContinue == null) {
                return;
            }

            AdTween.ExecuteSafeInUpdate(() => {
                try {
                    new Action<bool>(OnContinue)?.Invoke(success);
                }
                catch (Exception e) {
                    Debug.LogException(e);
                }
                OnContinue = null;
            });
        }

        // private IEnumerator DoShowAd() {
        //     // UNDONE: loading
        //     OnLoadingChange?.Invoke(true);
        //     yield return new WaitForSeconds(1.5f);

        //     interstitialAd.Show();
        // }

        /// <summary>
        /// Request lại với trì hoãn một khoảng thời gian
        /// </summary>
        /// <returns></returns>
        private IEnumerator RequestAfterDelay(float delay) {
            float curDelay = 0;
            while (curDelay < delay) {
                if (isRequestedAndWaitForResponing || isReadyToShow)
                    break;
                curDelay += Time.deltaTime;
                yield return null;
            }

            if (!isRequestedAndWaitForResponing && !isReadyToShow) {
                RequestAndLoad();
            }
        }

        private void RequestWhenAdFailed() {
            float delay = 10 + 5 * requestFailedContinousCount;
            delay = Mathf.Min(delay, 120);

            StartCoroutine(RequestAfterDelay(delay));
        }
    }
}