/************************************************************************************

Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.  

See SampleFramework license.txt for license terms.  Unless required by applicable law 
or agreed to in writing, the sample code is provided “AS IS” WITHOUT WARRANTIES OR 
CONDITIONS OF ANY KIND, either express or implied.  See the license for specific 
language governing permissions and limitations under the license.

************************************************************************************/

using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
	public class TrainCrossingController : MonoBehaviour
	{
		[SerializeField] private AudioSource _audioSource = null;
		[SerializeField] private AudioClip[] _crossingSounds = null;
		[SerializeField] private MeshRenderer _lightSide1Renderer = null;
		[SerializeField] private MeshRenderer _lightSide2Renderer = null;
		[SerializeField] private SelectionCylinder _selectionCylinder = null;

		private Material _lightsSide1Mat;
		private Material _lightsSide2Mat;
		private int _colorId = Shader.PropertyToID("_Color");

		private Coroutine _xingAnimationCr = null;
		private InteractableTool _toolInteractingWithMe = null;

		private void Awake()
		{
			Assert.IsNotNull(_audioSource);
			Assert.IsNotNull(_crossingSounds);
			Assert.IsNotNull(_lightSide1Renderer);
			Assert.IsNotNull(_lightSide2Renderer);
			Assert.IsNotNull(_selectionCylinder);

			_lightsSide1Mat = _lightSide1Renderer.material;
			_lightsSide2Mat = _lightSide2Renderer.material;
		}

		private void OnDestroy()
		{
			if (_lightsSide1Mat != null)
			{
				Destroy(_lightsSide1Mat);
			}
			if (_lightsSide2Mat != null)
			{
				Destroy(_lightsSide2Mat);
			}
		}

		public void CrossingButtonStateChanged(InteractableStateArgs obj)
		{
			bool inActionState = obj.NewInteractableState == InteractableState.ActionState;
			if (inActionState)
			{
				ActivateTrainCrossing();
			}

			_toolInteractingWithMe = obj.NewInteractableState > InteractableState.Default ?
			  obj.Tool : null;
		}

		private void Update()
		{
			if (_toolInteractingWithMe == null)
			{
				_selectionCylinder.CurrSelectionState = SelectionCylinder.SelectionState.Off;
			}
			else
			{
				_selectionCylinder.CurrSelectionState = (
				  _toolInteractingWithMe.ToolInputState == ToolInputState.PrimaryInputDown ||
				  _toolInteractingWithMe.ToolInputState == ToolInputState.PrimaryInputDownStay)
				  ? SelectionCylinder.SelectionState.Highlighted
				  : SelectionCylinder.SelectionState.Selected;
			}
		}

		private void ActivateTrainCrossing()
		{
			int maxSoundIndex = _crossingSounds.Length - 1;
			var audioClip = _crossingSounds[(int)(Random.value * maxSoundIndex)];
			_audioSource.clip = audioClip;
			_audioSource.timeSamples = 0;
			_audioSource.Play();
			if (_xingAnimationCr != null)
			{
				StopCoroutine(_xingAnimationCr);
			}
			_xingAnimationCr = StartCoroutine(AnimateCrossing(audioClip.length * 0.75f));
		}

		private IEnumerator AnimateCrossing(float animationLength)
		{
			ToggleLightObjects(true);

			float animationEndTime = Time.time + animationLength;

			float lightBlinkDuration = animationLength * 0.1f;
			float lightBlinkStartTime = Time.time;
			float lightBlinkEndTime = Time.time + lightBlinkDuration;
			Material lightToBlinkOn = _lightsSide1Mat;
			Material lightToBlinkOff = _lightsSide2Mat;
			Color onColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			Color offColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);

			while (Time.time < animationEndTime)
			{
				float t = (Time.time - lightBlinkStartTime) / lightBlinkDuration;
				lightToBlinkOn.SetColor(_colorId, Color.Lerp(offColor, onColor, t));
				lightToBlinkOff.SetColor(_colorId, Color.Lerp(onColor, offColor, t));

				// switch which lights blink on and off when time runs out
				if (Time.time > lightBlinkEndTime)
				{
					Material temp = lightToBlinkOn;
					lightToBlinkOn = lightToBlinkOff;
					lightToBlinkOff = temp;
					lightBlinkStartTime = Time.time;
					lightBlinkEndTime = Time.time + lightBlinkDuration;
				}

				yield return null;
			}

			ToggleLightObjects(false);
		}

		private void AffectMaterials(Material[] materials, Color newColor)
		{
			foreach (var material in materials)
			{
				material.SetColor(_colorId, newColor);
			}
		}

		private void ToggleLightObjects(bool enableState)
		{
			_lightSide1Renderer.gameObject.SetActive(enableState);
			_lightSide2Renderer.gameObject.SetActive(enableState);
		}
	}
}
