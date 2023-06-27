/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Integrations;
using UnityEngine;
using UnityEngine.UI;

namespace Meta.WitAi.TTS.Samples
{
    public class TTSCacheToggle : MonoBehaviour
    {
        // UI references
        [SerializeField] private TTSDiskCache _diskCache;
        [SerializeField] private Text _cacheLabel;
        [SerializeField] private Button _button;

        // Current disk cache location
        private TTSDiskCacheLocation _cacheLocation = (TTSDiskCacheLocation) (-1);

        // Add listeners
        private void OnEnable()
        {
            // Obtain disk cache if possible
            if (_diskCache == null)
            {
                _diskCache = GameObject.FindObjectOfType<TTSDiskCache>();
            }
            // Reset location text
            RefreshLocation();
            _button.onClick.AddListener(ToggleCache);
        }
        // Current disk cache location
        private TTSDiskCacheLocation GetCurrentCacheLocation() => _diskCache == null ? TTSDiskCacheLocation.Stream : _diskCache.DiskCacheDefaultSettings.DiskCacheLocation;
        // Check for changes
        private void Update()
        {
            if (_cacheLocation != GetCurrentCacheLocation())
            {
                RefreshLocation();
            }
        }
        // Refresh location & button text
        private void RefreshLocation()
        {
            _cacheLocation = GetCurrentCacheLocation();
            _cacheLabel.text = $"Disk Cache: {_cacheLocation}";
        }
        // Remove listeners
        private void OnDisable()
        {
            _button.onClick.RemoveListener(ToggleCache);
        }
        // Toggle cache
        public void ToggleCache()
        {
            // Toggle to next option
            TTSDiskCacheLocation cacheLocation = GetCurrentCacheLocation();
            switch (cacheLocation)
            {
                case TTSDiskCacheLocation.Stream:
                    cacheLocation = TTSDiskCacheLocation.Temporary;
                    break;
                case TTSDiskCacheLocation.Temporary:
                    cacheLocation = TTSDiskCacheLocation.Persistent;
                    break;
                case TTSDiskCacheLocation.Persistent:
                    cacheLocation = TTSDiskCacheLocation.Preload;
                    break;
                default:
                    cacheLocation = TTSDiskCacheLocation.Stream;
                    break;
            }

            // Set next option
            _diskCache.DiskCacheDefaultSettings.DiskCacheLocation = cacheLocation;
            // Clear runtime cache
            TTSService.Instance.UnloadAll();

            // Refresh location
            RefreshLocation();
        }
    }
}
