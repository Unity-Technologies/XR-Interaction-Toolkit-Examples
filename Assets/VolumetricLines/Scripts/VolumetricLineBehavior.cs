using UnityEngine;

namespace VolumetricLines
{
	/// <summary>
	/// Render a single volumetric line
	/// 
	/// Based on the Volumetric lines algorithm by Sebastien Hillaire
	/// http://sebastien.hillaire.free.fr/index.php?option=com_content&view=article&id=57&Itemid=74
	/// 
	/// Thread in the Unity3D Forum:
	/// http://forum.unity3d.com/threads/181618-Volumetric-lines
	/// 
	/// Unity3D port by Johannes Unterguggenberger
	/// johannes.unterguggenberger@gmail.com
	/// 
	/// Thanks to Michael Probst for support during development.
	/// 
	/// Thanks for bugfixes and improvements to Unity Forum User "Mistale"
	/// http://forum.unity3d.com/members/102350-Mistale
    /// 
    /// Shader code optimization and cleanup by Lex Darlog (aka DRL)
    /// http://forum.unity3d.com/members/lex-drl.67487/
    /// 
	/// </summary>
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
	[ExecuteInEditMode]
	public class VolumetricLineBehavior : MonoBehaviour 
	{
		// Used to compute the average value of all the Vector3's components:
		static readonly Vector3 Average = new Vector3(1f/3f, 1f/3f, 1f/3f);

		#region private variables
		/// <summary>
		/// Template material to be used
		/// </summary>
		[SerializeField]
		public Material m_templateMaterial;

		/// <summary>
		/// Set to false in order to change the material's properties as specified in this script.
		/// Set to true in order to *initially* leave the material's properties as they are in the template material.
		/// </summary>
		[SerializeField] 
		private bool m_doNotOverwriteTemplateMaterialProperties;

		/// <summary>
		/// The start position relative to the GameObject's origin
		/// </summary>
		[SerializeField] 
		private Vector3 m_startPos;
		
		/// <summary>
		/// The end position relative to the GameObject's origin
		/// </summary>
		[SerializeField] 
		private Vector3 m_endPos = new Vector3(0f, 0f, 100f);

		/// <summary>
		/// Line Color
		/// </summary>
		[SerializeField] 
		private Color m_lineColor;

		/// <summary>
		/// The width of the line
		/// </summary>
		[SerializeField] 
		private float m_lineWidth;

		/// <summary>
		/// Light saber factor
		/// </summary>
		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float m_lightSaberFactor;

		/// <summary>
		/// This GameObject's specific material
		/// </summary>
		private Material m_material;
		
		/// <summary>
		/// This GameObject's mesh filter
		/// </summary>
		private MeshFilter m_meshFilter;
		#endregion

		#region properties
		/// <summary>
		/// Gets or sets the tmplate material.
		/// Setting this will only have an impact once. 
		/// Subsequent changes will be ignored.
		/// </summary>
		public Material TemplateMaterial
		{
			get { return m_templateMaterial; }
			set { m_templateMaterial = value; }
		}

		/// <summary>
		/// Gets or sets whether or not the template material properties
		/// should be used (false) or if the properties of this MonoBehavior
		/// instance should be used (true, default).
		/// Setting this will only have an impact once, and then only if it
		/// is set before TemplateMaterial has been assigned.
		/// </summary>
		public bool DoNotOverwriteTemplateMaterialProperties
		{
			get { return m_doNotOverwriteTemplateMaterialProperties; }
			set { m_doNotOverwriteTemplateMaterialProperties = value; }
		}
		
		/// <summary>
		/// Get or set the line color of this volumetric line's material
		/// </summary>
		public Color LineColor
		{
			get { return m_lineColor;  }
			set
			{
				CreateMaterial();
				if (null != m_material)
				{
					m_lineColor = value;
					m_material.color = m_lineColor;
				}
			}
		}

		/// <summary>
		/// Get or set the line width of this volumetric line's material
		/// </summary>
		public float LineWidth
		{
			get { return m_lineWidth; }
			set
			{
				CreateMaterial();
				if (null != m_material)
				{
					m_lineWidth = value;
					m_material.SetFloat("_LineWidth", m_lineWidth);
				}
				UpdateBounds();
			}
		}

		/// <summary>
		/// Get or set the light saber factor of this volumetric line's material
		/// </summary>
		public float LightSaberFactor
		{
			get { return m_lightSaberFactor; }
			set
			{
				CreateMaterial();
				if (null != m_material)
				{
					m_lightSaberFactor = value;
					m_material.SetFloat("_LightSaberFactor", m_lightSaberFactor);
				}
			}
		}

		/// <summary>
		/// Get or set the start position of this volumetric line's mesh
		/// </summary>
		public Vector3 StartPos
		{
			get { return m_startPos; }
			set
			{
				m_startPos = value;
				SetStartAndEndPoints(m_startPos, m_endPos);
			}
		}

		/// <summary>
		/// Get or set the end position of this volumetric line's mesh
		/// </summary>
		public Vector3 EndPos
		{
			get { return m_endPos; }
			set
			{
				m_endPos = value;
				SetStartAndEndPoints(m_startPos, m_endPos);
			}
		}

		#endregion
		
		#region methods
		/// <summary>
		/// Creates a copy of the template material for this instance
		/// </summary>
		private void CreateMaterial()
		{
			if (null == m_material || null == GetComponent<MeshRenderer>().sharedMaterial)
			{
				if (null != m_templateMaterial)
				{
					m_material = Material.Instantiate(m_templateMaterial);
					GetComponent<MeshRenderer>().sharedMaterial = m_material;
					SetAllMaterialProperties();
				}
				else 
				{
					m_material = GetComponent<MeshRenderer>().sharedMaterial;
				}
			}
		}

		/// <summary>
		/// Destroys the copy of the template material which was used for this instance
		/// </summary>
		private void DestroyMaterial()
		{
			if (null != m_material)
			{
				DestroyImmediate(m_material);
				m_material = null;
			}
		}

		/// <summary>
		/// Calculates the (approximated) _LineScale factor based on the object's scale.
		/// </summary>
		private float CalculateLineScale()
		{
			return Vector3.Dot(transform.lossyScale, Average);
		}

		/// <summary>
		/// Updates the line scaling of this volumetric line based on the current object scaling.
		/// </summary>
		public void UpdateLineScale()
		{
			if (null != m_material) 
			{
				m_material.SetFloat("_LineScale", CalculateLineScale());
			}
		}

		/// <summary>
		/// Sets all material properties (color, width, light saber factor, start-, endpos)
		/// </summary>
		private void SetAllMaterialProperties()
		{
			SetStartAndEndPoints(m_startPos, m_endPos);

			if (null != m_material)
			{
				if (!m_doNotOverwriteTemplateMaterialProperties)
				{
					m_material.color = m_lineColor;
					m_material.SetFloat("_LineWidth", m_lineWidth);
					m_material.SetFloat("_LightSaberFactor", m_lightSaberFactor);
				}
				UpdateLineScale();
			}
		}

		/// <summary>
		/// Calculate the bounds of this line based on start and end points,
		/// the line width, and the scaling of the object.
		/// </summary>
		private Bounds CalculateBounds()
		{
			var maxWidth = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
			var scaledLineWidth = maxWidth * LineWidth * 0.5f;

			var min = new Vector3(
				Mathf.Min(m_startPos.x, m_endPos.x) - scaledLineWidth,
				Mathf.Min(m_startPos.y, m_endPos.y) - scaledLineWidth,
				Mathf.Min(m_startPos.z, m_endPos.z) - scaledLineWidth
			);
			var max = new Vector3(
				Mathf.Max(m_startPos.x, m_endPos.x) + scaledLineWidth,
				Mathf.Max(m_startPos.y, m_endPos.y) + scaledLineWidth,
				Mathf.Max(m_startPos.z, m_endPos.z) + scaledLineWidth
			);
			
			return new Bounds
			{
				min = min,
				max = max
			};
		}

		/// <summary>
		/// Updates the bounds of this line according to the current properties, 
		/// which there are: start point, end point, line width, scaling of the object.
		/// </summary>
		public void UpdateBounds()
		{
			if (null != m_meshFilter)
			{
				var mesh = m_meshFilter.sharedMesh;
				Debug.Assert(null != mesh);
				if (null != mesh)
				{
					mesh.bounds = CalculateBounds();
				}
			}
		}

		/// <summary>
		/// Sets the start and end points - updates the data of the Mesh.
		/// </summary>
		public void SetStartAndEndPoints(Vector3 startPoint, Vector3 endPoint)
		{
			m_startPos = startPoint;
			m_endPos = endPoint;

			Vector3[] vertexPositions = {
				m_startPos,
				m_startPos,
				m_startPos,
				m_startPos,
				m_endPos,
				m_endPos,
				m_endPos,
				m_endPos,
			};
			
			Vector3[] other = {
				m_endPos,
				m_endPos,
				m_endPos,
				m_endPos,
				m_startPos,
				m_startPos,
				m_startPos,
				m_startPos,
			};

			if (null != m_meshFilter)
			{
				var mesh = m_meshFilter.sharedMesh;
				Debug.Assert(null != mesh);
				if (null != mesh)
				{
					mesh.vertices = vertexPositions;
					mesh.normals = other;
					UpdateBounds();
				}
			}
		}
		#endregion

		#region event functions
		void Start () 
		{
			Mesh mesh = new Mesh();
			m_meshFilter = GetComponent<MeshFilter>();
			m_meshFilter.mesh = mesh;
			SetStartAndEndPoints(m_startPos, m_endPos);
			mesh.uv = VolumetricLineVertexData.TexCoords;
			mesh.uv2 = VolumetricLineVertexData.VertexOffsets;
			mesh.SetIndices(VolumetricLineVertexData.Indices, MeshTopology.Triangles, 0);
			CreateMaterial();
			// TODO: Need to set vertices before assigning new Mesh to the MeshFilter's mesh property => Why?
		}

		void OnDestroy()
		{
			if (null != m_meshFilter) 
			{
				if (Application.isPlaying) 
				{
					Mesh.Destroy(m_meshFilter.sharedMesh);
				}
				else // avoid "may not be called from edit mode" error
				{
					Mesh.DestroyImmediate(m_meshFilter.sharedMesh);
				}
				m_meshFilter.sharedMesh = null;
			}
			DestroyMaterial();
		}
		
		void Update()
		{
			if (transform.hasChanged)
			{
				UpdateLineScale();
				UpdateBounds();
			}
		}

		void OnValidate()
		{
			// This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only).
			//  => make sure, everything stays up-to-date
			if(string.IsNullOrEmpty(gameObject.scene.name) || string.IsNullOrEmpty(gameObject.scene.path)) {
				return; // ...but not if a Prefab is selected! (Only if we're using it within a scene.)
			}
			CreateMaterial();
			SetAllMaterialProperties();
			UpdateBounds();
		}
	
		void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(gameObject.transform.TransformPoint(m_startPos), gameObject.transform.TransformPoint(m_endPos));
		}
		#endregion
	}
}