/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;
using Meta.WitAi.TTS.Data;
using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities;

namespace Meta.WitAi.TTS.Samples
{
    public class TTSStatusLabel : MonoBehaviour
    {
        [SerializeField] private TTSSpeaker _speaker;
        [SerializeField] private Text _label;

        private void OnEnable()
        {
            RefreshLabel();
            _speaker.Events.OnClipDataLoadBegin.AddListener(OnClipRefresh);
            _speaker.Events.OnClipDataLoadAbort.AddListener(OnClipRefresh);
            _speaker.Events.OnClipDataLoadFailed.AddListener(OnClipRefresh);
            _speaker.Events.OnClipDataLoadSuccess.AddListener(OnClipRefresh);
            _speaker.Events.OnClipDataQueued.AddListener(OnClipRefresh);
            _speaker.Events.OnClipDataPlaybackReady.AddListener(OnClipRefresh);
            _speaker.Events.OnClipDataPlaybackStart.AddListener(OnClipRefresh);
            _speaker.Events.OnClipDataPlaybackFinished.AddListener(OnClipRefresh);
            _speaker.Events.OnClipDataPlaybackCancelled.AddListener(OnClipRefresh);
        }
        private void OnClipRefresh(TTSClipData clipData)
        {
            RefreshLabel();
        }
        private void OnDisable()
        {
            _speaker.Events.OnClipDataQueued.RemoveListener(OnClipRefresh);
            _speaker.Events.OnClipDataLoadBegin.RemoveListener(OnClipRefresh);
            _speaker.Events.OnClipDataLoadAbort.RemoveListener(OnClipRefresh);
            _speaker.Events.OnClipDataLoadFailed.RemoveListener(OnClipRefresh);
            _speaker.Events.OnClipDataLoadSuccess.RemoveListener(OnClipRefresh);
            _speaker.Events.OnClipDataPlaybackReady.RemoveListener(OnClipRefresh);
            _speaker.Events.OnClipDataPlaybackStart.RemoveListener(OnClipRefresh);
            _speaker.Events.OnClipDataPlaybackFinished.RemoveListener(OnClipRefresh);
            _speaker.Events.OnClipDataPlaybackCancelled.RemoveListener(OnClipRefresh);
        }

        private void RefreshLabel()
        {
            StringBuilder status = new StringBuilder();
            if (_speaker.SpeakingClip != null)
            {
                status.AppendLine($"Speaking: {_speaker.IsSpeaking}");
            }
            int index = 0;
            foreach (var clip in _speaker.QueuedClips)
            {
                status.Insert(0, $"Queue[{index}]: {clip.loadState.ToString()}\n");
                index++;
            }
            if (status.Length > 0)
            {
                status.Remove(status.Length - 1, 1);
            }
            _label.text = status.ToString();
        }
    }
}
