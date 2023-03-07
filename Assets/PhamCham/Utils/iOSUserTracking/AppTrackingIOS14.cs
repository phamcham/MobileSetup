using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


#if UNITY_IOS
using UnityEngine.iOS;
using Unity.Advertisement.IosSupport;
#endif

/// <summary>
/// This component will trigger the context screen to appear when the scene starts,
/// if the user hasn't already responded to the iOS tracking dialog.
/// </summary>
///
namespace PhamCham.Utils.iOS {
    public class AppTrackingIOS14 : MonoBehaviour {
        [SerializeField] private UnityEvent OnTrackingCompleted;

        private void Awake() {
#if UNITY_IOS && !UNITY_EDITOR
		Version expectedVersion = new Version("14.5");
		Version curVersion = new Version(Device.systemVersion);
		// apptracking just allowed on ios 14.5
		if (curVersion >= expectedVersion) {
			// check with iOS to see if the user has accepted or declined tracking
			var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();

			if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED) {
				Debug.Log("Unity iOS Support: Requesting iOS App Tracking Transparency native dialog.");
				ATTrackingStatusBinding.RequestAuthorizationTracking();
			}
			StartCoroutine(WaitTracking());
		}
		else {
			Debug.Log("iOS version lower 14.5");
			OnTrackingCompleted?.Invoke();
		}
#else
            Debug.Log("Unity iOS Support: App Tracking Transparency status not checked, because the platform is not iOS.");
            OnTrackingCompleted?.Invoke();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
		private IEnumerator WaitTracking()
		{
			yield return new WaitWhile(() =>
			{
				var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
				Debug.Log("ATTrackingStatusBinding: " + status);
				return (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED);
			});
			OnTrackingCompleted?.Invoke();
		}
#endif
    }
}