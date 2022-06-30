using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class OVRShaderBuildProcessor : IPreprocessShaders
{
	public int callbackOrder { get { return 0; } }

	public void OnProcessShader(
		Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> shaderCompilerData)
	{
		var projectConfig = OVRProjectConfig.GetProjectConfig();
		if (projectConfig == null)
		{
			return;
		}

		if (!projectConfig.skipUnneededShaders)
		{
			return;
		}

		if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
		{
			return;
		}

		var strippedGraphicsTiers = new HashSet<GraphicsTier>();

		// Unity only uses shader Tier2 on Quest and Go (regardless of graphics API)
		if (projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest) || 
			projectConfig.targetDeviceTypes.Contains(OVRProjectConfig.DeviceType.Quest2))
		{
			strippedGraphicsTiers.Add(GraphicsTier.Tier1);
			strippedGraphicsTiers.Add(GraphicsTier.Tier3);
		}

		if (strippedGraphicsTiers.Count == 0)
		{
			return;
		}

		for (int i = shaderCompilerData.Count - 1; i >= 0; --i)
		{
			if (strippedGraphicsTiers.Contains(shaderCompilerData[i].graphicsTier))
			{
				shaderCompilerData.RemoveAt(i);
			}
		}
	}
}
