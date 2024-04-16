using UnityEngine;
using System.Collections;

namespace VolumetricLines
{
	/// <summary>
	/// Render a line strip consisting of multiple VolumetricLineBehavior pieces stitched together
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
	public class VolumetricMultiLineBehavior : MonoBehaviour 
	{
		#region private variables
		/// <summary>
		/// Template material to be used for all line parts of this multiline
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
		/// Line Color for all line parts of this multiline
		/// </summary>
		[SerializeField]
		private Color m_lineColor;

		/// <summary>
		/// The width of all line parts of this multiline
		/// </summary>
		[SerializeField]
		private float m_lineWidth;

		/// <summary>
		/// Light saber factor for all line parts of this multiline
		/// </summary>
		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float m_lightSaberFactor;
		
		/// <summary>
		/// The vertices where 2 adjacent multi lines touch each other. 
		/// The end of line 1 is the start of line 2, etc.
		/// </summary>
		[SerializeField]
		private Vector3[] m_lineVertices;

		/// <summary>
		/// Contains all volumetric lines
		/// </summary>
		private VolumetricLineBehavior[] m_volumetricLines;
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
		/// Get or set the line color for each line part of this multiline
		/// </summary>
		public Color LineColor
		{
			get
			{
				return m_lineColor;
			}
			set
			{
				m_lineColor = value;
				if (null == m_volumetricLines)
				{
					return;
				}
				for (int i = 0; i < m_volumetricLines.Length; ++i)
				{
					if (null != m_volumetricLines[i] && m_volumetricLines[i])
					{
						m_volumetricLines[i].LineColor = value;
					}
				}
			}
		}

		/// <summary>
		/// Get or set the line width of this volumetric line's material
		/// </summary>
		public float LineWidth
		{
			get
			{
				return m_lineWidth;
			}
			set
			{
				m_lineWidth = value;
				if (null == m_volumetricLines)
				{
					return;
				}
				for (int i = 0; i < m_volumetricLines.Length; ++i)
				{
					if (null != m_volumetricLines[i] && m_volumetricLines[i])
					{
						m_volumetricLines[i].LineWidth = value;
					}
				}
			}
		}

		/// <summary>
		/// Get or set the light saber factor of this volumetric line's material
		/// </summary>
		public float LightSaberFactor
		{
			get
			{
				return m_lightSaberFactor;
			}
			set
			{
				m_lightSaberFactor = value;
				if (null == m_volumetricLines)
				{
					return;
				}
				for (int i = 0; i < m_volumetricLines.Length; ++i)
				{
					if (null != m_volumetricLines[i] && m_volumetricLines[i])
					{
						m_volumetricLines[i].LightSaberFactor = value;
					}
				}
			}
		}
		#endregion
		
		#region methods
		/// <summary>
		/// Instantiate all line parts of this multiline and set their properties
		/// </summary>
		public void CreateAllVolumetricLines()
		{
			if (null != m_volumetricLines)
			{
				return;
			}
			
			m_volumetricLines = new VolumetricLineBehavior[m_lineVertices.Length - 1];
			for (int i = 0; i < m_lineVertices.Length - 1; ++i)
			{
				int n = i;
				var go = new GameObject("multiline" + n);
				go.transform.SetParent(gameObject.transform);
				go.transform.localPosition = Vector3.zero;
				go.transform.localRotation = Quaternion.identity;

				var volLine = go.AddComponent<VolumetricLineBehavior>();
				volLine.TemplateMaterial = TemplateMaterial;
				volLine.DoNotOverwriteTemplateMaterialProperties = DoNotOverwriteTemplateMaterialProperties;
				volLine.LineWidth = LineWidth;
				volLine.LineColor = LineColor;
				volLine.LightSaberFactor = LightSaberFactor;
				volLine.StartPos = m_lineVertices[i];
				volLine.EndPos = m_lineVertices[i + 1];
				
				m_volumetricLines[i] = volLine;
			}
		}

		/// <summary>
		/// Destroys all line parts of this multiline
		/// </summary>
		public void DestroyAllVolumetricLines()
		{
			if (null == m_volumetricLines)
			{
				return;
			}

			for (int i = 0; i < m_volumetricLines.Length; ++i)
			{
				if (null == m_volumetricLines[i] || !m_volumetricLines[i])
				{
					continue;
				}
				var o = m_volumetricLines[i].gameObject;
				if (o)
				{
					GameObject.Destroy(o);
				}
			}
			m_volumetricLines = null;
		}

		/// <summary>
		/// Update the vertices of this multi line.
		/// </summary>
		/// <param name="newSetOfVertices">New set of vertices.</param>
		public void UpdateLineVertices(Vector3[] newSetOfVertices)
		{
			DestroyAllVolumetricLines();
			m_lineVertices = newSetOfVertices;
			CreateAllVolumetricLines();
		}

		/// <summary>
		/// Sets all material properties (color, width, light saber factor)
		/// </summary>
		private void SetAllMaterialProperties()
		{
			LineColor = LineColor;
			LineWidth = LineWidth;
			LightSaberFactor = LightSaberFactor;
		}
		#endregion

		#region event functions

		void Start () 
		{
			CreateAllVolumetricLines();
		}
		
		void OnDestroy()
		{
			DestroyAllVolumetricLines();
		}

		void OnValidate()
		{
			SetAllMaterialProperties();
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