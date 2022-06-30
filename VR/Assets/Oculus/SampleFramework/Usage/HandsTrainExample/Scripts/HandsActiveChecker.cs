/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

See SampleFramework license.txt for license terms.  Unless required by applicable law
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific
language governing permissions and limitations under the license.

************************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class HandsActiveChecker : MonoBehaviour
{
	[SerializeField]
	private GameObject _notificationPrefab = null;

	private GameObject _notification = null;
	private OVRCameraRig _cameraRig = null;
	private Transform _centerEye = null;

	private void Awake()
	{
		Assert.IsNotNull(_notificationPrefab);
		_notification = Instantiate(_notificationPrefab);
		StartCoroutine(GetCenterEye());
	}

	private void Update()
	{
		if (OVRPlugin.GetHandTrackingEnabled())
		{
			_notification.SetActive(false);
		}
		else
		{
			_notification.SetActive(true);
			if (_centerEye) {
				_notification.transform.position = _centerEye.position + _centerEye.forward * 0.5f;
				_notification.transform.rotation = _centerEye.rotation;
			}
			
		}

	}

	private IEnumerator GetCenterEye()
	{
		if ((_cameraRig = FindObjectOfType<OVRCameraRig>()) != null)
		{
			while (!_centerEye)
			{
				_centerEye = _cameraRig.centerEyeAnchor;
				yield return null;
			}
		}
	}
}
