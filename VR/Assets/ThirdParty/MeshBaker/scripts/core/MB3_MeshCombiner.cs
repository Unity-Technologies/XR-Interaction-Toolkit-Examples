using UnityEngine;
using System.Collections;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Text;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core {

    //TODO bug with triangles if using showHide with AddDelete reproduce by using the AddDeleteParts script and changeing some of it to show hide
    [System.Serializable]
    public abstract class MB3_MeshCombiner : MB_IMeshBakerSettings
    {
        public delegate void GenerateUV2Delegate(Mesh m, float hardAngle, float packMargin);

        public class MBBlendShapeKey
        {
            public GameObject gameObject;
            public int blendShapeIndexInSrc;

            public MBBlendShapeKey(GameObject srcSkinnedMeshRenderGameObject, int blendShapeIndexInSource)
            {
                gameObject = srcSkinnedMeshRenderGameObject;
                blendShapeIndexInSrc = blendShapeIndexInSource;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MBBlendShapeKey) || obj == null)
                {
                    return false;
                }
                MBBlendShapeKey other = (MBBlendShapeKey)obj;
                return (gameObject == other.gameObject && blendShapeIndexInSrc == other.blendShapeIndexInSrc);
            }

            public override int GetHashCode()
            {
                int hash = 23;
                unchecked
                {
                    hash = hash * 31 + gameObject.GetInstanceID();
                    hash = hash * 31 + blendShapeIndexInSrc;
                }
                return hash;
            }
        }

        public class MBBlendShapeValue
        {
            public GameObject combinedMeshGameObject;
            public int blendShapeIndex;
        }

        public static bool EVAL_VERSION {
            get { return false; }
        }

        [SerializeField] protected MB2_ValidationLevel _validationLevel = MB2_ValidationLevel.robust;
        public virtual MB2_ValidationLevel validationLevel
        {
            get { return _validationLevel; }
            set { _validationLevel = value; }
        }

        [SerializeField] protected string _name;
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }

        [SerializeField] protected MB2_TextureBakeResults _textureBakeResults;
        public virtual MB2_TextureBakeResults textureBakeResults
        {
            get { return _textureBakeResults; }
            set { _textureBakeResults = value; }
        }

        [SerializeField] protected GameObject _resultSceneObject;
        public virtual GameObject resultSceneObject
        {
            get { return _resultSceneObject; }
            set { _resultSceneObject = value; }
        }

        [SerializeField] protected UnityEngine.Renderer _targetRenderer;
        public virtual Renderer targetRenderer
        {
            get { return _targetRenderer; }
            set
            {
                if (_targetRenderer != null && _targetRenderer != value)
                {
                    Debug.LogWarning("Previous targetRenderer was not null. Combined mesh may be shared by more than one Renderer");
                }

                _targetRenderer = value;

                if (value != null && MB_Utility.IsSceneInstance(value.gameObject) && value.transform.parent != null)
                {
                    _resultSceneObject = value.transform.parent.gameObject;
                }
            }
        }

        [SerializeField] protected MB2_LogLevel _LOG_LEVEL = MB2_LogLevel.info;
        public virtual MB2_LogLevel LOG_LEVEL
        {
            get { return _LOG_LEVEL; }
            set { _LOG_LEVEL = value; }
        }

        public MB_IMeshBakerSettings settings
        {
            get
            {
                if (_settingsHolder != null)
                {
                    return settingsHolder.GetMeshBakerSettings();
                }
                else
                {
                    return this;
                }
            }
        }

        /// <summary>
        /// This needs to be an Object so it gets serialized and works with SerializedProperty. 
        /// Would like this to be of type MB_IMeshBakerSettingsHolder
        /// </summary>
        [SerializeField] protected UnityEngine.Object _settingsHolder;
        public virtual MB_IMeshBakerSettingsHolder settingsHolder
        {
            get {
                if (_settingsHolder != null)
                {
                    if (_settingsHolder is MB_IMeshBakerSettingsHolder)
                    {
                        return (MB_IMeshBakerSettingsHolder)_settingsHolder;
                    } else {
                        _settingsHolder = null;
                    }
                }
                return null;
            }
            set
            {
                if (value is UnityEngine.Object)
                {
                    _settingsHolder = (UnityEngine.Object)value;
                } else
                {
                    Debug.LogError("The settings holder must be a UnityEngine.Object");
                }
            }
        }

        //-----------------------
        [SerializeField] protected MB2_OutputOptions _outputOption;

        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.outputOption NOT this.outputOption THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual MB2_OutputOptions outputOption
        {
            get { return _outputOption; }
            set { _outputOption = value; }
        }

        [SerializeField] protected MB_RenderType _renderType;

        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.outputOption NOT this.outputOption THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual MB_RenderType renderType {
            get { return _renderType; }
            set { _renderType = value; }
        }

        [SerializeField] protected MB2_LightmapOptions _lightmapOption = MB2_LightmapOptions.ignore_UV2;

        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.lightmapOption NOT this.lightmapOption THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual MB2_LightmapOptions lightmapOption {
            get { return _lightmapOption; }
            set {
                _lightmapOption = value; 
            }
        }

        [SerializeField] protected bool _doNorm = true;

        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doNorm NOT this.doNorm THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doNorm {
            get { return _doNorm; }
            set { _doNorm = value; }
        }


        [SerializeField] protected bool _doTan = true;

        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doTan NOT this.doTan THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doTan {
            get { return _doTan; }
            set { _doTan = value; }
        }

        [SerializeField] protected bool _doCol;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doCol NOT this.doCol THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doCol {
            get { return _doCol; }
            set { _doCol = value; }
        }

        [SerializeField] protected bool _doUV = true;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doUV NOT this.doUV THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doUV {
            get { return _doUV; }
            set { _doUV = value; }
        }

        /// <summary>
        /// only included for backward compatibility. Does nothing
        /// </summary>
        public virtual bool doUV1 {
            get { return false; }
            set { }
        }

        public virtual bool doUV2() {
            bool result = settings.lightmapOption == MB2_LightmapOptions.copy_UV2_unchanged || settings.lightmapOption == MB2_LightmapOptions.preserve_current_lightmapping || settings.lightmapOption == MB2_LightmapOptions.copy_UV2_unchanged_to_separate_rects;
            return result;
        }


        [SerializeField] protected bool _doUV3;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doUV3 NOT this.doUV3 THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doUV3 {
            get { return _doUV3; }
            set { _doUV3 = value; }
        }

        [SerializeField] protected bool _doUV4;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doUV4 NOT this.doUV4 THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doUV4 {
            get { return _doUV4; }
            set { _doUV4 = value; }
        }

        [SerializeField] protected bool _doUV5;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doUV5 NOT this.doUV5 THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doUV5
        {
            get { return _doUV5; }
            set { _doUV5 = value; }
        }

        [SerializeField] protected bool _doUV6;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doUV6 NOT this.doUV6 THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doUV6
        {
            get { return _doUV6; }
            set { _doUV6 = value; }
        }

        [SerializeField] protected bool _doUV7;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doUV7 NOT this.doUV7 THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doUV7
        {
            get { return _doUV7; }
            set { _doUV7 = value; }
        }

        [SerializeField] protected bool _doUV8;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doUV8 NOT this.doUV8 THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doUV8
        {
            get { return _doUV8; }
            set { _doUV8 = value; }
        }

        [SerializeField]
        protected bool _doBlendShapes;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.doBlendShapes NOT this.doBlendShapes THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool doBlendShapes
        {
            get { return _doBlendShapes; }
            set { _doBlendShapes = value; }
        }

        [UnityEngine.Serialization.FormerlySerializedAs("_recenterVertsToBoundsCenter")]
        [SerializeField]
        protected MB_MeshPivotLocation _pivotLocationType;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.pivotLocationType NOT this.pivotLocationType THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual MB_MeshPivotLocation pivotLocationType
        {
            get { return _pivotLocationType; }
            set { _pivotLocationType = value; }
        }

        [SerializeField]
        protected Vector3 _pivotLocation;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.pivotLocation NOT this.pivotLocation THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual Vector3 pivotLocation
        {
            get { return _pivotLocation; }
            set { _pivotLocation = value; }
        }

        [SerializeField]
        protected bool _clearBuffersAfterBake = false;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.clearBuffersAfterBake NOT this.clearBuffersAfterBake THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public virtual bool clearBuffersAfterBake
        {
            get { return _clearBuffersAfterBake; }
            set {
                Debug.LogError("Not implemented.");
                _clearBuffersAfterBake = value;
            }
        }

        [SerializeField]
        public bool _optimizeAfterBake = true;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.optimizeAfterBake NOT this.optimizeAfterBake THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public bool optimizeAfterBake
        {
            get { return _optimizeAfterBake; }
            set { _optimizeAfterBake = value; }
        }

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("uv2UnwrappingParamsHardAngle")]
        protected float _uv2UnwrappingParamsHardAngle = 60f;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.uv2UnwrappingParamsHardAngle NOT this.uv2UnwrappingParamsHardAngle THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public float uv2UnwrappingParamsHardAngle
        {
            get { return _uv2UnwrappingParamsHardAngle; }
            set { _uv2UnwrappingParamsHardAngle = value; }
        }

        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("uv2UnwrappingParamsPackMargin")]
        protected float _uv2UnwrappingParamsPackMargin = .005f;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.uv2UnwrappingParamsPackMargin NOT this.uv2UnwrappingParamsPackMargin THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public float uv2UnwrappingParamsPackMargin
        {
            get { return _uv2UnwrappingParamsPackMargin; }
            set { _uv2UnwrappingParamsPackMargin = value; }
        }

        [SerializeField]
        protected bool _smrNoExtraBonesWhenCombiningMeshRenderers;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.smrNoExtraBonesWhenCombiningMeshRenderers NOT this.smrNoExtraBonesWhenCombiningMeshRenderers THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public bool smrNoExtraBonesWhenCombiningMeshRenderers
        {
            get { return _smrNoExtraBonesWhenCombiningMeshRenderers; }
            set { _smrNoExtraBonesWhenCombiningMeshRenderers = value; }
        }

        [SerializeField]
        protected bool _smrMergeBlendShapesWithSameNames = false;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.smrMergeBlendShapesWithSameNames NOT this.smrMergeBlendShapesWithSameNames THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public bool smrMergeBlendShapesWithSameNames
        {
            get { return _smrMergeBlendShapesWithSameNames; }
            set { _smrMergeBlendShapesWithSameNames = value; }
        }

        [SerializeField]
        protected UnityEngine.Object _assignToMeshCustomizer;
        /// <summary>
        /// ALWAYS ACCESS THROUGH this.settings.assignToMeshCustomizer NOT this.assignToMeshCustomizer THERE MAY BE A SETTINGS HOLDER ASSIGNED.
        /// </summary>
        public IAssignToMeshCustomizer assignToMeshCustomizer
        {
            get
            {
                if (_assignToMeshCustomizer is IAssignToMeshCustomizer)
                {
                    return (IAssignToMeshCustomizer)_assignToMeshCustomizer;
                }
                else
                {
                    _assignToMeshCustomizer = null;
                    return null;
                }
            }
            set
            {
                _assignToMeshCustomizer = (UnityEngine.Object) value;
            }
        }

        protected bool _usingTemporaryTextureBakeResult;
        public abstract int GetLightmapIndex();
		public abstract void ClearBuffers();
		public abstract void ClearMesh();
        public abstract void ClearMesh(MB2_EditorMethodsInterface editorMethods);
        public abstract void DisposeRuntimeCreated();
        public abstract void DestroyMesh();
		public abstract void DestroyMeshEditor(MB2_EditorMethodsInterface editorMethods);
		public abstract List<GameObject> GetObjectsInCombined();
		public abstract int GetNumObjectsInCombined();
		public abstract int GetNumVerticesFor(GameObject go);
		public abstract int GetNumVerticesFor(int instanceID);

        /// <summary>
        /// Builds a map for mapping blend shapes in the source SkinnedMeshRenderers to blend shapes in the
        /// combined skinned meshes. If you need to serialize the map then use: BuildSourceBlendShapeToCombinedSerializableIndexMap.
        /// </summary>
        [System.Obsolete("BuildSourceBlendShapeToCombinedIndexMap is deprecated. The map will be attached to the combined SkinnedMeshRenderer object as the MB_BlendShape2CombinedMap Component.")]
        public abstract Dictionary<MBBlendShapeKey, MBBlendShapeValue> BuildSourceBlendShapeToCombinedIndexMap();

        /// <summary>
        /// Copies Mesh Baker internal data to the mesh.
        /// </summary>		
        public virtual void Apply(){
			Apply(null);
		}
		
		/// <summary>
		/// Copies Mesh Baker internal data to the mesh.
		/// </summary>
		/// <param name='uv2GenerationMethod'>
		/// Uv2 generation method. This is normally editor class method Unwrapping.GenerateSecondaryUVSet
		/// </param>
		public abstract void Apply(GenerateUV2Delegate uv2GenerationMethod);

        /// <summary>
        /// Apply the specified triangles, vertices, normals, tangents, uvs, colors, uv1, uv2, bones and uv2GenerationMethod.
        /// </summary>
        /// <param name='triangles'>
        /// Triangles.
        /// </param>
        /// <param name='vertices'>
        /// Vertices.
        /// </param>
        /// <param name='normals'>
        /// Normals.
        /// </param>
        /// <param name='tangents'>
        /// Tangents.
        /// </param>
        /// <param name='uvs'>
        /// Uvs.
        /// </param>
        /// <param name='colors'>
        /// Colors.
        /// </param>
        /// <param name='uv3'>
        /// Uv3.
        /// </param>
        /// <param name='uv4'>
        /// Uv4.
        /// </param>
        /// <param name='uv2'>
        /// Uv2.
        /// </param>
        /// <param name='bones'>
        /// Bones.
        /// </param>
        /// <param name='uv2GenerationMethod'>
        /// Uv2 generation method. This is normally method Unwrapping.GenerateSecondaryUVSet. This should be null when calling Apply at runtime.
        /// </param>
        public abstract void Apply(bool triangles,
                          bool vertices,
                          bool normals,
                          bool tangents,
                          bool uvs,
                          bool uv2,
                          bool uv3,
                          bool uv4,
                          bool uv5,
                          bool uv6,
                          bool uv7,
                          bool uv8,
                          bool colors,
                          bool bones = false,
                          bool blendShapeFlag = false,
                          GenerateUV2Delegate uv2GenerationMethod = null);

        /// <summary>
        /// Apply the specified triangles, vertices, normals, tangents, uvs, colors, uv1, uv2, bones and uv2GenerationMethod.
        /// This is the pre 2018.2 version that does not suport eight UV channels.
        /// </summary>
        /// <param name='triangles'>
        /// Triangles.
        /// </param>
        /// <param name='vertices'>
        /// Vertices.
        /// </param>
        /// <param name='normals'>
        /// Normals.
        /// </param>
        /// <param name='tangents'>
        /// Tangents.
        /// </param>
        /// <param name='uvs'>
        /// Uvs.
        /// </param>
        /// <param name='colors'>
        /// Colors.
        /// </param>
        /// <param name='uv3'>
        /// Uv3.
        /// </param>
        /// <param name='uv4'>
        /// Uv4.
        /// </param>
        /// <param name='uv2'>
        /// Uv2.
        /// </param>
        /// <param name='bones'>
        /// Bones.
        /// </param>
        /// <param name='uv2GenerationMethod'>
        /// Uv2 generation method. This is normally method Unwrapping.GenerateSecondaryUVSet. This should be null when calling Apply at runtime.
        /// </param>		
        public abstract void Apply(bool triangles,
						  bool vertices,
						  bool normals,
						  bool tangents,
						  bool uvs,
                          bool uv2,
                          bool uv3,
                          bool uv4,
                          bool colors,
						  bool bones=false,
                          bool blendShapeFlag=false,
						  GenerateUV2Delegate uv2GenerationMethod = null);


        public virtual bool UpdateGameObjects(GameObject[] gos)
        {
            return UpdateGameObjects(gos, true, true, true, true, true, false, false, false,
                                        false, false, false, false, false, false);
        }

        /// <summary>
        /// Updates the data in the combined mesh for meshes that are already in the combined mesh.
        /// This is faster than adding and removing a mesh and has a much lower memory footprint.
        /// This method can only be used if the meshes being updated have the same layout(number of 
        /// vertices, triangles, submeshes).
        /// This is faster than removing and re-adding
        /// For efficiency update as few channels as possible.
        /// Apply must be called to apply the changes to the combined mesh
        /// </summary>		
        public virtual bool UpdateGameObjects(GameObject[] gos, bool updateBounds)
        {
            return UpdateGameObjects(gos, updateBounds, true, true, true, true, false, false, false, false, false, false, false, false, false);
        }

        /// <summary>
        /// Updates the data in the combined mesh for meshes that are already in the combined mesh.
        /// This is faster than adding and removing a mesh and has a much lower memory footprint.
        /// This method can only be used if the meshes being updated have the same layout(number of 
        /// vertices, triangles, submeshes).
        /// This is faster than removing and re-adding
        /// For efficiency update as few channels as possible.
        /// Apply must be called to apply the changes to the combined mesh
        /// </summary>		
        public abstract bool UpdateGameObjects(GameObject[] gos, bool recalcBounds,
                                        bool updateVertices, bool updateNormals, bool updateTangents,
                                        bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4,
										bool updateColors, bool updateSkinningInfo);

        /// <summary>
        /// Updates the data in the combined mesh for meshes that are already in the combined mesh.
        /// This is faster than adding and removing a mesh and has a much lower memory footprint.
        /// This method can only be used if the meshes being updated have the same layout(number of 
        /// vertices, triangles, submeshes).
        /// This is faster than removing and re-adding
        /// For efficiency update as few channels as possible.
        /// Apply must be called to apply the changes to the combined mesh
        /// </summary>		
        public abstract bool UpdateGameObjects(GameObject[] gos, bool recalcBounds,
                                        bool updateVertices, bool updateNormals, bool updateTangents,
                                        bool updateUV, bool updateUV2, bool updateUV3, bool updateUV4,
                                        bool updateUV5, bool updateUV6, bool updateUV7, bool updateUV8,
                                        bool updateColors, bool updateSkinningInfo);

        public abstract bool AddDeleteGameObjects(GameObject[] gos, GameObject[] deleteGOs, bool disableRendererInSource=true);

		public abstract bool AddDeleteGameObjectsByID(GameObject[] gos, int[] deleteGOinstanceIDs, bool disableRendererInSource);
		public abstract bool CombinedMeshContains(GameObject go);
		public abstract void UpdateSkinnedMeshApproximateBounds();
		public abstract void UpdateSkinnedMeshApproximateBoundsFromBones();
        public abstract void CheckIntegrity();

        /// <summary>
        /// Updates the skinned mesh approximate bounds from the bounds of the source objects.
        /// </summary>		
        public abstract void UpdateSkinnedMeshApproximateBoundsFromBounds();
		
		/// <summary>
		/// Updates the skinned mesh bounds by creating a bounding box that contains the bones (skeleton) of the source objects.
		/// </summary>		
		public static void UpdateSkinnedMeshApproximateBoundsFromBonesStatic(Transform[] bs, SkinnedMeshRenderer smr){
			Vector3 max, min;
			max = bs[0].position;
			min = bs[0].position;
			for (int i = 1; i < bs.Length; i++){
				Vector3 v = bs[i].position;
				if (v.x < min.x) min.x = v.x;
				if (v.y < min.y) min.y = v.y;
				if (v.z < min.z) min.z = v.z;
				if (v.x > max.x) max.x = v.x;
				if (v.y > max.y) max.y = v.y;
				if (v.z > max.z) max.z = v.z;			
			}
			Vector3 center = (max + min)/2f;
			Vector3 size = max - min;
			Matrix4x4 w2l = smr.worldToLocalMatrix;
			Bounds b = new Bounds(w2l * center, w2l * size);		
			smr.localBounds = b;
		}

		public static void UpdateSkinnedMeshApproximateBoundsFromBoundsStatic(List<GameObject> objectsInCombined,SkinnedMeshRenderer smr){
			Bounds b = new Bounds();
			Bounds bigB = new Bounds();
			if (MB_Utility.GetBounds(objectsInCombined[0],out b)){
				bigB = b;
			} else {
				Debug.LogError("Could not get bounds. Not updating skinned mesh bounds");	
				return;
			}
			for (int i = 1; i < objectsInCombined.Count; i++){
				if (MB_Utility.GetBounds(objectsInCombined[i],out b)){
					bigB.Encapsulate(b);
				} else {
					Debug.LogError("Could not get bounds. Not updating skinned mesh bounds");	
					return;					
				}
			}	
			smr.localBounds = bigB;			
		}		

		protected virtual bool _CreateTemporaryTextrueBakeResult(GameObject[] gos, List<Material> matsOnTargetRenderer){ 
            if (GetNumObjectsInCombined() > 0)
            {
                Debug.LogError("Can't add objects if there are already objects in combined mesh when 'Texture Bake Result' is not set. Perhaps enable 'Clear Buffers After Bake'");
                return false;
            }
			_usingTemporaryTextureBakeResult = true;
			_textureBakeResults = MB2_TextureBakeResults.CreateForMaterialsOnRenderer(gos, matsOnTargetRenderer);
			return true;
		}

        public abstract List<Material> GetMaterialsOnTargetRenderer();

    }
}