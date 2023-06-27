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

using UnityEngine;
using UnityEngine.Profiling;

namespace Oculus.Interaction.UnityCanvas
{
    public class CanvasMeshRenderer : MonoBehaviour
    {
        private static readonly int MainTexShaderID = Shader.PropertyToID("_MainTex");

        [Tooltip("The canvas texture that will be rendered.")]
        [SerializeField]
        protected CanvasRenderTexture _canvasRenderTexture;

        [Tooltip("The mesh renderer that will be driven.")]
        [SerializeField]
        protected MeshRenderer _meshRenderer;

        [Tooltip("Determines the shader used for rendering. " +
            "See the documentation for details on these rendering modes.")]
        [SerializeField]
        protected int _renderingMode = (int)RenderingMode.AlphaCutout;

        [Tooltip("Requires MSAA. Provides limited transparency useful for " +
                 "anti-aliasing soft edges of UI elements.")]
        [SerializeField]
        private bool _useAlphaToMask = true;

        [Tooltip("Select the alpha cutoff used for the cutout rendering.")]
        [Range(0, 1)]
        [SerializeField]
        private float _alphaCutoutThreshold = 0.5f;

        private RenderingMode RenderingMode => (RenderingMode)_renderingMode;

        protected virtual string GetShaderName()
        {
            switch (RenderingMode)
            {
                case RenderingMode.AlphaBlended:
                    return "Hidden/Imposter_AlphaBlended";
                case RenderingMode.AlphaCutout:
                    if (_useAlphaToMask)
                    {
                        return "Hidden/Imposter_AlphaToMask";
                    }
                    else
                    {
                        return "Hidden/Imposter_AlphaCutout";
                    }
                default:
                case RenderingMode.Opaque:
                    return "Hidden/Imposter_Opaque";
            }
        }

        protected virtual void SetAdditionalProperties(MaterialPropertyBlock block)
        {
            block.SetFloat("_Cutoff", GetAlphaCutoutThreshold());
        }

        protected virtual float GetAlphaCutoutThreshold()
        {
            if (RenderingMode == RenderingMode.AlphaCutout &&
                !_useAlphaToMask)
            {
                return _alphaCutoutThreshold;
            }
            return 1f;
        }

        protected Material _material;
        protected bool _started;

        protected virtual void HandleUpdateRenderTexture(Texture texture)
        {
            _meshRenderer.material = _material;
            var block = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(block);
            block.SetTexture(MainTexShaderID, texture);
            SetAdditionalProperties(block);
            _meshRenderer.SetPropertyBlock(block);
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_meshRenderer, nameof(_meshRenderer));
            this.AssertField(_canvasRenderTexture, nameof(_canvasRenderTexture));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Profiler.BeginSample("InterfaceRenderer.UpdateMaterial");
                try
                {
                    _material = new Material(Shader.Find(GetShaderName()));
                }
                finally
                {
                    Profiler.EndSample();
                }

                _canvasRenderTexture.OnUpdateRenderTexture += HandleUpdateRenderTexture;
                if (_canvasRenderTexture.Texture != null)
                {
                    HandleUpdateRenderTexture(_canvasRenderTexture.Texture);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_material != null)
                {
                    Destroy(_material);
                    _material = null;
                }
                _canvasRenderTexture.OnUpdateRenderTexture -= HandleUpdateRenderTexture;
            }
        }

        public static partial class Properties
        {
            public static readonly string RenderingMode = nameof(_renderingMode);
            public static readonly string UseAlphaToMask = nameof(_useAlphaToMask);
            public static readonly string AlphaCutoutThreshold = nameof(_alphaCutoutThreshold);
        }

        #region Inject
        public void InjectAllCanvasMeshRenderer(CanvasRenderTexture canvasRenderTexture,
                                                MeshRenderer meshRenderer)
        {
            InjectCanvasRenderTexture(canvasRenderTexture);
            InjectMeshRenderer(meshRenderer);
        }

        public void InjectCanvasRenderTexture(CanvasRenderTexture canvasRenderTexture)
        {
            _canvasRenderTexture = canvasRenderTexture;
        }

        public void InjectMeshRenderer(MeshRenderer meshRenderer)
        {
            _meshRenderer = meshRenderer;
        }

        public void InjectOptionalRenderingMode(RenderingMode renderingMode)
        {
            _renderingMode = (int)renderingMode;
        }

        public void InjectOptionalAlphaCutoutThreshold(float alphaCutoutThreshold)
        {
            _alphaCutoutThreshold = alphaCutoutThreshold;
        }

        public void InjectOptionalUseAlphaToMask(bool useAlphaToMask)
        {
            _useAlphaToMask = useAlphaToMask;
        }
        #endregion
    }
}
