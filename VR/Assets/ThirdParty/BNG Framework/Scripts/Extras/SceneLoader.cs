using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BNG {
    public class SceneLoader : MonoBehaviour {

        [Tooltip("The Unity 'LoadSceneMode' method of loading the scene (In most cases should be 'Single'). ")]
        public LoadSceneMode loadSceneMode = LoadSceneMode.Single;

        [Tooltip("If true, the ScreenFader component will fade the screen to black before loading a level.")]
        public bool UseSceenFader = true;

        [Tooltip("Wait this long in seconds before attempting to load the scene. Useful if you need to fade the screen out before attempting to load the level.")]
        public float ScreenFadeTime = 0.5f;

        ScreenFader sf;

        private string _loadSceneName = string.Empty;

        public void LoadScene(string SceneName) {
            if(UseSceenFader) {
                _loadSceneName = SceneName;

                StartCoroutine("FadeThenLoadScene");
            }
        }

        public IEnumerator FadeThenLoadScene() {

            if (UseSceenFader) {
                if (sf == null) {
                    sf = FindObjectOfType<ScreenFader>();
                    // May not have found anything
                    if (sf != null) {
                        sf.DoFadeIn();
                    }
                }
            }

            if(ScreenFadeTime > 0) {
                yield return new WaitForSeconds(ScreenFadeTime);
            }

            SceneManager.LoadScene(_loadSceneName, loadSceneMode);
        }
    }
}

