using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;

using UnityEngine;

// Class that manages loading of Ktx Textures through OVRPlugin
public class OVRKtxTexture
{
	private const uint KTX_TTF_BC7_RGBA = 6;
	private const uint KTX_TTF_ASTC_4x4_RGBA = 10;

	public static bool Load(byte[] data, ref OVRTextureData ktxData)
	{
		int unmanagedSize = Marshal.SizeOf(data[0]) * data.Length;
		IntPtr dataPtr = Marshal.AllocHGlobal(unmanagedSize);

		Marshal.Copy(data, 0, dataPtr, data.Length);
		IntPtr ktxTexturePtr = OVRPlugin.Ktx.LoadKtxFromMemory(dataPtr, (uint)data.Length);
		Marshal.FreeHGlobal(dataPtr);

		ktxData.width = (int)OVRPlugin.Ktx.GetKtxTextureWidth(ktxTexturePtr);
		ktxData.height = (int)OVRPlugin.Ktx.GetKtxTextureHeight(ktxTexturePtr);

		bool transcodeResult = false;
#if UNITY_ANDROID && !UNITY_EDITOR
		transcodeResult = OVRPlugin.Ktx.TranscodeKtxTexture(ktxTexturePtr, KTX_TTF_ASTC_4x4_RGBA);
		ktxData.transcodedFormat = TextureFormat.ASTC_4x4;
#else
		transcodeResult = OVRPlugin.Ktx.TranscodeKtxTexture(ktxTexturePtr, KTX_TTF_BC7_RGBA);
		ktxData.transcodedFormat = TextureFormat.BC7;
#endif
		if (!transcodeResult)
		{
			Debug.LogError("Failed to transcode KTX texture.");
			return false;
		}

		uint textureSize = OVRPlugin.Ktx.GetKtxTextureSize(ktxTexturePtr);
		IntPtr textureDataPtr = Marshal.AllocHGlobal(sizeof(byte) * (int)textureSize);
		if(!OVRPlugin.Ktx.GetKtxTextureData(ktxTexturePtr, textureDataPtr, textureSize))
		{
			Debug.LogError("Failed to get texture data from Ktx texture reference");
			return false;
		}

		byte[] textureData = new byte[textureSize];
		Marshal.Copy(textureDataPtr, textureData, 0, textureData.Length);
		Marshal.FreeHGlobal(textureDataPtr);
		ktxData.data = textureData;

		OVRPlugin.Ktx.DestroyKtxTexture(ktxTexturePtr);

		return true;
	}
}
