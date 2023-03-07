using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.Events;

namespace PhamCham.GoogleMobileAds {

    public class RewardedAdUnit : AdUnit {
#pragma warning disable 414 // disable adUnitId not use in other platform
#if UNITY_IOS
        private const string adUnitTestId = "ca-app-pub-3940256099942544/1712485313";
#else
        private const string adUnitTestId = "ca-app-pub-3940256099942544/5224354917";
#endif
        // khoang cach giua cac lan show quang cao
        // luu y: request van giu nguyen, chi kiem tra thoi gian show giua cac quang cao
        [Header("Interval")]
        [SerializeField] protected float intervalBetweenAds = 20f;
        [SerializeField] protected float delayFirstRequestCall = 30f;

        // [Header("Events")]
        // [SerializeField] private UnityEvent<bool> OnLoadingChange;
        // [SerializeField] private UnityEvent<bool> OnSoundTempChange;

        private RewardedAd rewardedAd;
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
        private Action OnContinue;

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

            AdTween.DelayCallTween(delayFirstRequestCall, RequestAndLoad);
        }

        private void RequestAndLoad() {
#if UNITY_ANDROID
            string adUnitId = adUnitId_Android.Trim();
#else
            string adUnitId = adUnitId_IOS.Trim();
#endif
            if (isTesting)
                adUnitId = adUnitTestId.Trim();

            // Clean up before using it
            if (rewardedAd != null) {
                rewardedAd.Destroy();
            }

            AdRequest adRequest = new AdRequest.Builder().Build();
            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError loadError) => {
                if (loadError != null) {
                    Debugger.Log(this, () => "Rewarded ad failed to load with error: " + loadError.GetMessage());
                    HandleAdFailedToLoadEvent();
                    return;
                }
                else if (ad == null) {
                    Debugger.Log(this, () => "Rewarded ad failed to load.");
                    HandleAdFailedToLoadEvent();
                    return;
                }

                Debugger.Log(this, () => "Rewarded ad loaded.");
                rewardedAd = ad;
                HandleAdLoadedEvent();

                ad.OnAdFullScreenContentOpened += () => {
                    Debugger.Log(this, () => "Rewarded ad opening.");
                    HandleAdOpeningEvent();
                };
                ad.OnAdFullScreenContentClosed += () => {
                    Debugger.Log(this, () => "Rewarded ad closed.");
                    HandleAdClosedEvent(true);
                };
                ad.OnAdImpressionRecorded += () => {
                    Debugger.Log(this, () => "Rewarded ad recorded an impression.");
                };
                ad.OnAdClicked += () => {
                    Debugger.Log(this, () => "Rewarded ad recorded a click.");
                };
                ad.OnAdFullScreenContentFailed += (AdError error) => {
                    Debugger.LogWarning(this, () => "Rewarded ad failed to show with error: " + error.GetMessage());
                    HandleAdFailedToShowEvent();
                };
                ad.OnAdPaid += (AdValue adValue) => {
                    string msg = string.Format("{0} (currency: {1}, value: {2})",
                                               "Rewarded ad received a paid event.",
                                               adValue.CurrencyCode,
                                               adValue.Value);
                    Debug.Log(msg);
                };
            });

            isRequestedAndWaitForResponing = true;
        }

        private void HandleAdClosedEvent(bool success) {
            AdTween.ExecuteSafeInUpdate(() => {
                isRequestedAndWaitForResponing = false;
                isReadyToShow = false;
                intervalBetweenAdsTime = DateTime.Now;

                if (success) {
                    requestFailedContinousCount = 0;

                    GoogleMobileAdsManager.AdDelayer.DelaySomeSeconds();
                    StartCoroutine(RequestAfterDelay(5));
                }

                // OnSoundTempChange?.Invoke(true);
                AdUtils.OnSoundTempChange(true);
                // OnLoadingChange?.Invoke(false);
                AdUtils.OnLoadingChange(false);

                ExecuteContinue();
            });
        }

        private void HandleAdOpeningEvent() {
            isRequestedAndWaitForResponing = false;
            isReadyToShow = false;

            AdTween.ExecuteSafeInUpdate(() => AdUtils.OnSoundTempChange(false));
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

                ExecuteContinue();

                RequestWhenAdFailed();
            });
        }

        private void HandleAdFailedToLoadEvent() {
            isRequestedAndWaitForResponing = false;
            isReadyToShow = false;

            requestFailedContinousCount = Mathf.Min(10, requestFailedContinousCount + 1);

            RequestWhenAdFailed();
        }

        // qc tra thuong xet ca truong hop checkinterval, vi isadavailable dung de check button
        public bool IsAdAvailable() {
            bool hasInternet = Application.internetReachability != NetworkReachability.NotReachable;
            if (!hasInternet) {
                Debugger.LogWarning(this, () => "[RewardedAd] unavailable cause no internet");
                return false;
            }

            bool rewardedLoaded = rewardedAd != null && rewardedAd.CanShowAd();
            if (!rewardedLoaded) {
                Debugger.LogWarning(this, () => "[RewardedAd] unavailable cause ad unloaded");
                return false;
            }

            bool intervalBetweenAdsEnough = CheckInterval();
            if (!intervalBetweenAdsEnough) {
                Debugger.LogWarning(this, () => "[RewardedAd] unavailable cause has delay between open ads");
                return false;
            }

            bool hasDelayAds = GoogleMobileAdsManager.AdDelayer.IsDelaying();
            if (hasDelayAds) {
                Debugger.LogWarning(this, () => "[RewardedAd] unavailable cause has delay between all types ads");
                return false;
            }

            return true;
        }

        public void ShowAd(Action onUserEarnedReward, Action onContinue) {
            OnContinue = onContinue;

            if (IsAdAvailable()) {
                // OnLoadingChange?.Invoke(true);
                AdUtils.OnLoadingChange(true);
                GoogleMobileAdsManager.AdDelayer.DelaySomeSeconds();
                AdTween.DelayCallTween(1.5f, () => {
                    rewardedAd.Show(reward => {
                        AdTween.ExecuteSafeInUpdate(onUserEarnedReward);
                    });
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

        private void ExecuteContinue() {
            AdTween.ExecuteSafeInUpdate(() => {
                try {
                    OnContinue?.Invoke();
                }
                catch (Exception ex) {
                    Debug.LogException(ex);
                }
                OnContinue = null;
            });
        }

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
            float delay = 20 + 5 * requestFailedContinousCount;
            delay = Mathf.Min(delay, 180);

            StartCoroutine(RequestAfterDelay(delay));
        }
    }
}