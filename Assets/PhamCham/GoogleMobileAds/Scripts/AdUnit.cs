using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhamCham.GoogleMobileAds {
    public abstract class AdUnit : MonoBehaviour {
        [Header("Testing")]
        [SerializeField] protected bool isTesting = false;

        [Header("Unit ID")]
        [SerializeField] protected string adUnitId_Android = "";

        [SerializeField] protected string adUnitId_IOS = "";

        public abstract void Initialize();
    }

}