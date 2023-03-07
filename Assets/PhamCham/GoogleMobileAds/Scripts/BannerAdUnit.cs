using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.Events;

namespace PhamCham.GoogleMobileAds {
    public class BannerAdUnit : AdUnit {
#pragma warning disable 414 // disable adUnitId not use in other platform
#if UNITY_ANDROID
        private const string adUnitTestId = "ca-app-pub-3940256099942544/6300978111";
#else
        private const string adUnitTestId = "ca-app-pub-3940256099942544/2934735716";
#endif

        private BannerView bannerView;
        private bool isShowing = false;
        private int failedCount = 0;

        public override void Initialize() {
            if (AdUtils.IsRemoveAds()) {
                return;
            }

            RequestBanner();
        }

        private void RequestBanner() {
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

            // DestroyBanner();

            if (bannerView != null) {
                bannerView.Destroy();
                bannerView = null;
            }

            AdSize adaptiveSize =
                AdSize.GetPortraitAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            bannerView = new BannerView(adUnitId, adaptiveSize, AdPosition.Bottom);

            bannerView.OnBannerAdLoaded += () => {
                Debugger.Log(this, () => "[Banner ad] loaded.");

                if (isShowing) {
                    ShowBanner();
                }

                failedCount = 0;
            };
            bannerView.OnBannerAdLoadFailed += (LoadAdError error) => {
                Debugger.Log(this, () => "[Banner ad] failed to load with error: " + error.GetMessage());
                HandleOnAdFailedToLoad();

                failedCount++;
            };
            bannerView.OnAdFullScreenContentOpened += () => {
                Debugger.Log(this, () => "[Banner ad] opening.");
            };
            bannerView.OnAdFullScreenContentClosed += () => {
                Debugger.Log(this, () => "[Banner ad] closed.");
            };
            bannerView.OnAdPaid += (AdValue adValue) => {
                string msg = string.Format("{0} (currency: {1}, value: {2}",
                                            "[Banner ad] received a paid event.",
                                            adValue.CurrencyCode,
                                            adValue.Value);
                Debugger.Log(this, () => msg);
            };

            // Create an empty ad request.
            AdRequest request = new AdRequest.Builder().Build();

            // Load the banner with the request.
            bannerView.LoadAd(request);

            HideBanner();
        }

        public void HandleOnAdFailedToLoad() {
            float delay = 10f + failedCount * 10f;
            Debugger.Log(this, () => "[Banner Ad] HandleOnAdFailedToLoad: " + delay + " seconds");
            //StartCoroutine(RequestAfterDelay(delay));
            AdTween.DelayCallTween(delay, RequestBanner);
        }

        public void HideBanner() {
            bannerView?.Hide();
        }

        public void ShowBanner() {
            Debugger.Log(this, () => "[Banner Ad] Call ShowBanner()");
            if (AdUtils.IsRemoveAds()) {
                return;
            }

            // request for sure
            if (bannerView == null) {
                RequestBanner();
            }

            bannerView.Show();
            isShowing = true;
        }

        /// <summary>
        /// use when no playing
        /// </summary>
        public void DestroyBanner() {
            if (bannerView != null) {
                bannerView.Destroy();
                bannerView = null;
            }
            isShowing = false;
        }

        private void OnApplicationPause(bool pause) {
            Debugger.Log(this, () => "[Banner Ad] OnApplicationPause: " + pause);
            if (AdUtils.IsRemoveAds()) {
                return;
            }

            if (pause) {
                bannerView?.Hide();
            }
            else {
                if (isShowing) {
                    // 2s sau neu van dang gameplay thi show
                    AdTween.DelayCallTween(2, () => {
                        if (isShowing && bannerView != null) {
                            bannerView.Show();
                        }
                    });
                }
            }
        }
    }
}