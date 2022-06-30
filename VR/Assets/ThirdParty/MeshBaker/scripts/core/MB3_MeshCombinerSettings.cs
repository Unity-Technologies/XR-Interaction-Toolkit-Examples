using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalOpus.MB.Core
{
    public enum MB_MeshPivotLocation
    {
        worldOrigin,
        boundsCenter,
        customLocation,
    }

    [System.Serializable]
    public class MB3_MeshCombinerSettingsData : MB_IMeshBakerSettings
    {
        [SerializeField] protected MB_RenderType _renderType;
        public virtual MB_RenderType renderType
        {
            get { return _renderType; }
            set { _renderType = value; }
        }

        [SerializeField] protected MB2_OutputOptions _outputOption;
        public virtual MB2_OutputOptions outputOption
        {
            get { return _outputOption; }
            set { _outputOption = value; }
        }

        [SerializeField] protected MB2_LightmapOptions _lightmapOption = MB2_LightmapOptions.ignore_UV2;
        public virtual MB2_LightmapOptions lightmapOption
        {
            get { return _lightmapOption; }
            set { _lightmapOption = value; }
        }

        [SerializeField] protected bool _doNorm = true;
        public virtual bool doNorm
        {
            get { return _doNorm; }
            set { _doNorm = value; }
        }

        [SerializeField] protected bool _doTan = true;
        public virtual bool doTan
        {
            get { return _doTan; }
            set { _doTan = value; }
        }

        [SerializeField] protected bool _doCol;
        public virtual bool doCol
        {
            get { return _doCol; }
            set { _doCol = value; }
        }

        [SerializeField] protected bool _doUV = true;
        public virtual bool doUV
        {
            get { return _doUV; }
            set { _doUV = value; }
        }

        [SerializeField] protected bool _doUV3;
        public virtual bool doUV3
        {
            get { return _doUV3; }
            set { _doUV3 = value; }
        }

        [SerializeField] protected bool _doUV4;
        public virtual bool doUV4
        {
            get { return _doUV4; }
            set { _doUV4 = value; }
        }

        [SerializeField] protected bool _doUV5;
        public virtual bool doUV5
        {
            get { return _doUV5; }
            set { _doUV5 = value; }
        }

        [SerializeField] protected bool _doUV6;
        public virtual bool doUV6
        {
            get { return _doUV6; }
            set { _doUV6 = value; }
        }

        [SerializeField] protected bool _doUV7;
        public virtual bool doUV7
        {
            get { return _doUV7; }
            set { _doUV7 = value; }
        }

        [SerializeField] protected bool _doUV8;
        public virtual bool doUV8
        {
            get { return _doUV8; }
            set { _doUV8 = value; }
        }

        [SerializeField]
        protected bool _doBlendShapes;
        public virtual bool doBlendShapes
        {
            get { return _doBlendShapes; }
            set { _doBlendShapes = value; }
        }

        [UnityEngine.Serialization.FormerlySerializedAs("_recenterVertsToBoundsCenter")]
        [SerializeField]
        protected MB_MeshPivotLocation _pivotLocationType;
        public virtual MB_MeshPivotLocation pivotLocationType
        {
            get { return _pivotLocationType; }
            set{ _pivotLocationType = value; }
        }

        [SerializeField]
        protected Vector3 _pivotLocation;
        public virtual Vector3 pivotLocation
        {
            get { return _pivotLocation; }
            set { _pivotLocation = value; }
        }

        [SerializeField]
        protected bool _clearBuffersAfterBake = false;
        public bool clearBuffersAfterBake
        {
            get { return _clearBuffersAfterBake; }
            set { _clearBuffersAfterBake = value; }
        }

        [SerializeField]
        public bool _optimizeAfterBake = true;
        public bool optimizeAfterBake
        {
            get { return _optimizeAfterBake; }
            set { _optimizeAfterBake = value; }
        }

        [SerializeField]
        protected float _uv2UnwrappingParamsHardAngle = 60f;
        public float uv2UnwrappingParamsHardAngle
        {
            get { return _uv2UnwrappingParamsHardAngle; }
            set { _uv2UnwrappingParamsHardAngle = value; }
        }

        [SerializeField]
        protected float _uv2UnwrappingParamsPackMargin = .005f;
        public float uv2UnwrappingParamsPackMargin
        {
            get { return _uv2UnwrappingParamsPackMargin; }
            set { _uv2UnwrappingParamsPackMargin = value; }
        }

        [SerializeField]
        protected bool _smrNoExtraBonesWhenCombiningMeshRenderers;
        public bool smrNoExtraBonesWhenCombiningMeshRenderers
        {
            get { return _smrNoExtraBonesWhenCombiningMeshRenderers; }
            set { _smrNoExtraBonesWhenCombiningMeshRenderers = value; }
        }

        [SerializeField]
        protected bool _smrMergeBlendShapesWithSameNames = false;
        public bool smrMergeBlendShapesWithSameNames
        {
            get { return _smrMergeBlendShapesWithSameNames; }
            set { _smrMergeBlendShapesWithSameNames = value; }
        }

        [SerializeField]
        protected UnityEngine.Object _assignToMeshCustomizer;
        public IAssignToMeshCustomizer assignToMeshCustomizer
        {
            get
            {
                if (_assignToMeshCustomizer is IAssignToMeshCustomizer)
                {
                    return (IAssignToMeshCustomizer) _assignToMeshCustomizer;
                }
                else
                {
                    _assignToMeshCustomizer = null;
                    return null;
                }
            }
            set
            {
                _assignToMeshCustomizer = (UnityEngine.Object)value;
            }
        }
    }

    [CreateAssetMenu(fileName = "MeshBakerSettings", menuName = "Mesh Baker/Mesh Baker Settings")]
    public class MB3_MeshCombinerSettings : ScriptableObject, MB_IMeshBakerSettingsHolder
    {
        public MB3_MeshCombinerSettingsData data;

        public MB_IMeshBakerSettings GetMeshBakerSettings()
        {
            return data;
        }
        public void GetMeshBakerSettingsAsSerializedProperty(out string propertyName, out UnityEngine.Object targetObj)
        {
            targetObj = this;
            propertyName = "data";
        }
    }
}
