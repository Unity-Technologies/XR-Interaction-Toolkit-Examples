using System.Collections.Generic;
using UnityEngine;

public class OVRResources : MonoBehaviour
{
	private static AssetBundle resourceBundle;
	private static List<string> assetNames;

	public static UnityEngine.Object Load(string path)
	{
		if (Debug.isDebugBuild)
		{
			if(resourceBundle == null)
			{
				Debug.Log("[OVRResources] Resource bundle was not loaded successfully");
				return null;
			}

			var result = assetNames.Find(s => s.Contains(path.ToLower()));
			return resourceBundle.LoadAsset(result);
		}
		return Resources.Load(path);
	}
	public static T Load<T>(string path) where T : UnityEngine.Object
	{
		if (Debug.isDebugBuild)
		{
			if (resourceBundle == null)
			{
				Debug.Log("[OVRResources] Resource bundle was not loaded successfully");
				return null;
			}

			var result = assetNames.Find(s => s.Contains(path.ToLower()));
			return resourceBundle.LoadAsset<T>(result);
		}
		return Resources.Load<T>(path);
	}

	public static void SetResourceBundle(AssetBundle bundle)
	{
		resourceBundle = bundle;
		assetNames = new List<string>();
		assetNames.AddRange(resourceBundle.GetAllAssetNames());
	}
}
