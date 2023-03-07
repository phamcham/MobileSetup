using System;
using System.Collections;
using System.Collections.Generic;
using BibleStudios.BibleVerse;
using UnityEngine;

namespace PhamCham.GoogleMobileAds {
    public class AdUtils {
        /// <summary>
        /// Only use for ad, dont use for gameplay
        /// </summary>
        public static bool IsRemoveAds() {
            return SaveDataExtension.Game.IsRemoveAds;
            //throw new NotImplementedException();
        }

        public static void OnLoadingChange(bool active) {
            if (active) {
                PopupManager.LoadingPanel.Open();
            }
            else {
                PopupManager.LoadingPanel.Close();
            }
        }

        public static void OnSoundTempChange(bool active) {
            AudioPlayer.SetActiveSoundTemporary(active);
        }
    }
}