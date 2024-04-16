using UnityEngine;

namespace VolumetricLines
{
	/// <summary>
	/// Render a line strip of volumetric lines
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
    /// /// Shader code optimization and cleanup by Lex Darlog (aka DRL)
    /// http://forum.unity3d.com/members/lex-drl.67487/
    /// 
	/// </summary>
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
	public class VolumetricLineStripBehavior : MonoBehaviour 
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
		
		/// <summary>
		/// The vertices of the line
		/// </summary>
		[SerializeField]
		private Vector3[] m_lineVertices;
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
			get { return m_lineColor; }
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
		/// Gets the vertices of this line strip
		/// </summary>
		public Vector3[] LineVertices
		{
			get { return m_lineVertices; }
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
		/// Sets all material properties (color, width, start-, endpos)
		/// </summary>
		private void SetAllMaterialProperties()
		{
			UpdateLineVertices(m_lineVertices);

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
		/// Calculate the bounds of this line based on the coordinates of the line vertices,
		/// the line width, and the scaling of the object.
		/// </summary>
		private Bounds CalculateBounds()
		{
			var maxWidth = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
			var scaledLineWidth = maxWidth * LineWidth * 0.5f;
			var scaledLineWidthVec = new Vector3(scaledLineWidth, scaledLineWidth, scaledLineWidth);

			Debug.Assert(m_lineVertices.Length > 0);
			if (m_lineVertices.Length == 0)
			{
				return new Bounds();
			}

			var min = m_lineVertices[0];
			var max = m_lineVertices[0];
			for (int i = 1; i < m_lineVertices.Length; ++i)
			{
				min = new Vector3(
					Mathf.Min(min.x, m_lineVertices[i].x),
					Mathf.Min(min.y, m_lineVertices[i].y),
					Mathf.Min(min.z, m_lineVertices[i].z)
				);
				max = new Vector3(
					Mathf.Max(max.x, m_lineVertices[i].x),
					Mathf.Max(max.y, m_lineVertices[i].y),
					Mathf.Max(max.z, m_lineVertices[i].z)
				);
			}

			return new Bounds
			{
				min = min - scaledLineWidthVec,
				max = max + scaledLineWidthVec
			};
		}

		/// <summary>
		/// Updates the bounds of this line according to the current properties, 
		/// which there are: coordinates of the line vertices, line width, scaling of the object.
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
		/// Updates the vertices of this VolumetricLineStrip.
		/// This is an expensive operation.
		/// </summary>
		/// <param name="newSetOfVertices">new set of vertices for the line strip.</param>
		public void UpdateLineVertices(Vector3[] newSetOfVertices)
		{
			if (null == newSetOfVertices)
			{
				return;
			}

			if (newSetOfVertices.Length < 3)
			{
				Debug.LogError("Add at least 3 vertices to the VolumetricLineStrip");
				return;
			}

			m_lineVertices = newSetOfVertices;

			// fill vertex positions, and indices
			// 2 for each position, + 2 for the start, + 2 for the end
			Vector3[] vertexPositions = new Vector3[m_lineVertices.Length * 2 + 4];
			// there are #vertices - 2 faces, and 3 indices each
			int[] indices = new int[(m_lineVertices.Length * 2 + 2) * 3];
			int v = 0;
			int x = 0;
			vertexPositions[v++] = m_lineVertices[0];
			vertexPositions[v++] = m_lineVertices[0];
			for (int i = 0; i < m_lineVertices.Length; ++i)
			{
				vertexPositions[v++] = m_lineVertices[i];
				vertexPositions[v++] = m_lineVertices[i];
				indices[x++] = v - 2;
				indices[x++] = v - 3;
				indices[x++] = v - 4;
				indices[x++] = v - 1;
				indices[x++] = v - 2;
				indices[x++] = v - 3;
			}
			vertexPositions[v++] = m_lineVertices[m_lineVertices.Length - 1];
			vertexPositions[v++] = m_lineVertices[m_lineVertices.Length - 1];
			indices[x++] = v - 2;
			indices[x++] = v - 3;
			indices[x++] = v - 4;
			indices[x++] = v - 1;
			indices[x++] = v - 2;
			indices[x++] = v - 3;

			// fill texture coordinates and vertex offsets
			Vector2[] texCoords = new Vector2[vertexPositions.Length];
			Vector2[] vertexOffsets = new Vector2[vertexPositions.Length];
			int t = 0;
			int o = 0;
			texCoords[t++] = new Vector2(1.0f, 0.0f);
			texCoords[t++] = new Vector2(1.0f, 1.0f);
			texCoords[t++] = new Vector2(0.5f, 0.0f);
			texCoords[t++] = new Vector2(0.5f, 1.0f);
			vertexOffsets[o++] = new Vector2(1.0f, -1.0f);
			vertexOffsets[o++] = new Vector2(1.0f, 1.0f);
			vertexOffsets[o++] = new Vector2(0.0f, -1.0f);
			vertexOffsets[o++] = new Vector2(0.0f, 1.0f);
			for (int i = 1; i < m_lineVertices.Length - 1; ++i)
			{
				if ((i & 0x1) == 0x1)
				{
					texCoords[t++] = new Vector2(0.5f, 0.0f);
					texCoords[t++] = new Vector2(0.5f, 1.0f);
				}
				else
				{
					texCoords[t++] = new Vector2(0.5f, 0.0f);
					texCoords[t++] = new Vector2(0.5f, 1.0f);
				}
				vertexOffsets[o++] = new Vector2(0.0f, 1.0f);
				vertexOffsets[o++] = new Vector2(0.0f, -1.0f);
			}
			texCoords[t++] = new Vector2(0.5f, 0.0f);
			texCoords[t++] = new Vector2(0.5f, 1.0f);
			texCoords[t++] = new Vector2(0.0f, 0.0f);
			texCoords[t++] = new Vector2(0.0f, 1.0f);
			vertexOffsets[o++] = new Vector2(0.0f, 1.0f);
			vertexOffsets[o++] = new Vector2(0.0f, -1.0f);
			vertexOffsets[o++] = new Vector2(1.0f, 1.0f);
			vertexOffsets[o++] = new Vector2(1.0f, -1.0f);


			// fill previous and next positions
			Vector3[] prevPositions = new Vector3[vertexPositions.Length];
			Vector4[] nextPositions = new Vector4[vertexPositions.Length];
			int p = 0;
			int n = 0;
			prevPositions[p++] = m_lineVertices[1];
			prevPositions[p++] = m_lineVertices[1];
			prevPositions[p++] = m_lineVertices[1];
			prevPositions[p++] = m_lineVertices[1];
			nextPositions[n++] = m_lineVertices[1];
			nextPositions[n++] = m_lineVertices[1];
			nextPositions[n++] = m_lineVertices[1];
			nextPositions[n++] = m_lineVertices[1];
			for (int i = 1; i < m_lineVertices.Length - 1; ++i)
			{
				prevPositions[p++] = m_lineVertices[i - 1];
				prevPositions[p++] = m_lineVertices[i - 1];
				nextPositions[n++] = m_lineVertices[i + 1];
				nextPositions[n++] = m_lineVertices[i + 1];
			}
			prevPositions[p++] = m_lineVertices[m_lineVertices.Length - 2];
			prevPositions[p++] = m_lineVertices[m_lineVertices.Length - 2];
			prevPositions[p++] = m_lineVertices[m_lineVertices.Length - 2];
			prevPositions[p++] = m_lineVertices[m_lineVertices.Length - 2];
			nextPositions[n++] = m_lineVertices[m_lineVertices.Length - 2];
			nextPositions[n++] = m_lineVertices[m_lineVertices.Length - 2];
			nextPositions[n++] = m_lineVertices[m_lineVertices.Length - 2];
			nextPositions[n++] = m_lineVertices[m_lineVertices.Length - 2];

			if (null != m_meshFilter)
			{
				var mesh = m_meshFilter.sharedMesh;
				Debug.Assert(null != mesh);
				if (null != mesh)
				{
					mesh.SetIndices(null, MeshTopology.Triangles, 0); // Reset before setting again to prevent a unity error message.
					mesh.vertices = vertexPositions;
					mesh.normals = prevPositions;
					mesh.tangents = nextPositions;
					mesh.uv = texCoords;
					mesh.uv2 = vertexOffsets;
					mesh.SetIndices(indices, MeshTopology.Triangles, 0);
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
			UpdateLineVertices(m_lineVertices);
			CreateMaterial();
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
			if (null == m_lineVertices)
			{
				return;
			}
			for (int i=0; i < m_lineVertices.Length - 1; ++i)
			{
				Gizmos.DrawLine(gameObject.transform.TransformPoint(m_lineVertices[i]), gameObject.transform.TransformPoint(m_lineVertices[i+1]));
			}
		}
		#endregion
	}
}