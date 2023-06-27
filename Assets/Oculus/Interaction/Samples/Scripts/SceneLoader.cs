/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oculus.Interaction.Samples
{
    public class SceneLoader : MonoBehaviour
    {
        private bool _loading = false;
        public Action<string> WhenLoadingScene = delegate { };
        public Action<string> WhenSceneLoaded = delegate { };
        private int _waitingCount = 0;

        public void Load(string sceneName)
        {
            if (_loading) return;
            _loading = true;
            // make sure we wait for all parties concerned to let us know they're ready to load
            _waitingCount = WhenLoadingScene.GetInvocationList().Length-1;  // remove the count for the blank delegate
            if (_waitingCount == 0)
            {
                // if nobody else cares just set the preload to go directly to the loading of the scene
                HandleReadyToLoad(sceneName);
            }
            else
            {
                WhenLoadingScene.Invoke(sceneName);
            }
        }

        // this should be called after handling any pre-load tasks (e.g. fade to white) by anyone who regsistered with WhenLoadingScene in order for the loading to proceed
        public void HandleReadyToLoad(string sceneName)
        {
            _waitingCount--;
            if (_waitingCount <= 0)
            {
                StartCoroutine(LoadSceneAsync(sceneName));
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            WhenSceneLoaded.Invoke(sceneName);
        }
    }
}
