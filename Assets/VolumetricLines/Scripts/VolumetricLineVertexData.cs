using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VolumetricLines
{
	public static class VolumetricLineVertexData
	{
		public static readonly Vector2[] TexCoords = {
			new Vector2(1.0f, 1.0f),
			new Vector2(1.0f, 0.0f),
			new Vector2(0.5f, 1.0f),
			new Vector2(0.5f, 0.0f),
			new Vector2(0.5f, 0.0f),
			new Vector2(0.5f, 1.0f),
			new Vector2(0.0f, 0.0f),
			new Vector2(0.0f, 1.0f),
		};


		public static readonly Vector2[] VertexOffsets = {
			 new Vector2(1.0f,   1.0f),
			 new Vector2(1.0f,  -1.0f),
			 new Vector2(0.0f,   1.0f),
			 new Vector2(0.0f,  -1.0f),
			 new Vector2(0.0f,   1.0f),
			 new Vector2(0.0f,  -1.0f),
			 new Vector2(1.0f,   1.0f),
			 new Vector2(1.0f,  -1.0f)
		};

		public static readonly int[] Indices =
		{
			2, 1, 0,
			3, 1, 2,
			4, 3, 2,
			5, 4, 2,
			4, 5, 6,
			6, 5, 7
		};
	}
}
