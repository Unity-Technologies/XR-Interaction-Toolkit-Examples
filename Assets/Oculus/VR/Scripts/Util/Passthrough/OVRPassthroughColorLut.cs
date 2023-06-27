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

using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class OVRPassthroughColorLut : System.IDisposable
{
    private const int RecomendedBatchSize = 128;

    public uint Resolution { get; private set; }
    public ColorChannels Channels { get; private set; }
    public bool IsInitialized { get; private set; }

    internal ulong _colorLutHandle;
    private GCHandle _allocHandle;
    private OVRPlugin.PassthroughColorLutData _lutData;
    private int _channelCount;
    private byte[] _colorBytes;
    private object _locker = new object();

    /// <summary>
    /// Initialize the color LUT data from a texture. Color channels are inferred from texture format.
    /// Use `UpdateFrom()` to update LUT data after construction.
    /// </summary>
    /// <param name="initialLutTexture">Texture to initialize the LUT from</param>
    /// <param name="flipY">Flag to inform whether the LUT texture should be flipped vertically. This is needed for LUT images which have color (0, 0, 0)
    /// in the top-left corner. Some color grading systems, e.g. Unity post-processing, have color (0, 0, 0) in the bottom-left corner,
    /// in which case flipping is not needed.</param>
    public OVRPassthroughColorLut(Texture2D initialLutTexture, bool flipY = true)
        : this(GetTextureSize(initialLutTexture), GetChannelsForTextureFormat(initialLutTexture.format))
    {
        Create(CreateLutDataFromTexture(initialLutTexture, flipY));
    }

    /// <summary>
    /// Set the color LUT data from an array of `Color`. The resolution is
    /// inferred from the array size, thus the size needs to be a result of
    /// `resolution = size ^ 3 * numColorChannels`, where `numColorChannels` depends
    /// on `channels`.
    /// Use `UpdateFrom()` to update color LUT data after construction.
    /// </summary>
    /// <param name="initialColorLut">Color array to initialize the LUT from</param>
    /// <param name="channels">Color channels for one color LUT entry</param>
    public OVRPassthroughColorLut(Color[] initialColorLut, ColorChannels channels)
        : this(GetArraySize(initialColorLut), channels)
    {
        Create(CreateLutDataFromArray(initialColorLut));
    }

    /// <summary>
    /// Set the color LUT data from an array of `Color`. The resolution is
    /// inferred from the array size, thus the size needs to be a result of
    /// `resolution = size ^ 3 * numColorChannels`, where `numColorChannels` depends
    /// on `channels`.
    /// Use `UpdateFrom()` to update color LUT data after construction.
    /// </summary>
    /// <param name="initialColorLut">Color32 array to initialize the LUT from</param>
    /// <param name="channels">Color channels for one color LUT entry</param>
    public OVRPassthroughColorLut(Color32[] initialColorLut, ColorChannels channels)
        : this(GetArraySize(initialColorLut), channels)
    {
        Create(CreateLutDataFromArray(initialColorLut));
    }

    /// <summary>
    /// Set the color LUT data from an array of `Color`. The resolution is
    /// inferred from the array size, thus the size needs to be a result of
    /// `resolution = size ^ 3 * numColorChannels`, where `numColorChannels` depends
    /// on `channels`.
    /// Use `UpdateFrom()` to update color LUT data after construction.
    /// </summary>
    /// <param name="initialColorLut">Color byte array to initialize the LUT from</param>
    /// <param name="channels">Color channels for one color LUT entry</param>
    public OVRPassthroughColorLut(byte[] initialColorLut, ColorChannels channels)
        : this(GetTextureSizeFromByteArray(initialColorLut, channels), channels)
    {
        Create(CreateLutDataFromArray(initialColorLut));
    }

    /// <summary>
    /// Update color LUT data from an array of Colors.
    /// Color channels and resolution must match the original.
    /// </summary>
    /// <param name="colors">Color array</param>
    public void UpdateFrom(Color[] colors)
    {
        if (IsValidLutUpdate(colors, _channelCount))
        {
            WriteColorsAsBytes(colors, _colorBytes);
            OVRPlugin.UpdatePassthroughColorLut(_colorLutHandle, _lutData);
        }
    }

    /// <summary>
    /// Update color LUT data from an array of Colors.
    /// Color channels and resolution must match the original.
    /// </summary>
    /// <param name="colors">Color array</param>
    public void UpdateFrom(Color32[] colors)
    {
        if (IsValidLutUpdate(colors, _channelCount))
        {
            WriteColorsAsBytes(colors, _colorBytes);
            OVRPlugin.UpdatePassthroughColorLut(_colorLutHandle, _lutData);
        }
    }

    /// <summary>
    /// Update color LUT data from an array of Colors.
    /// Color channels and resolution must match the original.
    /// </summary>
    /// <param name="colors">Color array</param>
    public void UpdateFrom(byte[] colors)
    {
        if (IsValidLutUpdate(colors, 1))
        {
            colors.CopyTo(_colorBytes, 0);
            OVRPlugin.UpdatePassthroughColorLut(_colorLutHandle, _lutData);
        }
    }

    /// <summary>
    /// Update color LUT data from a texture.
    /// Color channels and resolution must match the original.
    /// </summary>
    /// <param name="lutTexture">Color LUT texture</param>
    /// <param name="flipY">Flag to inform whether the LUT texture should be flipped vertically. This is needed for LUT images which have color (0, 0, 0)
    /// in the top-left corner. Some color grading systems, e.g. Unity post-processing, have color (0, 0, 0) in the bottom-left corner,
    /// in which case flipping is not needed.</param>
    public void UpdateFrom(Texture2D lutTexture, bool flipY = true)
    {
        if (IsValidUpdateResolution(GetTextureSize(lutTexture), _channelCount))
        {
            ColorLutTextureConverter.TextureToColorByteMap(lutTexture, _channelCount, _colorBytes, flipY);
            OVRPlugin.UpdatePassthroughColorLut(_colorLutHandle, _lutData);
        }
    }

    public void Dispose()
    {
        if (IsInitialized)
        {
            Destroy();
        }

        if (_allocHandle != null && _allocHandle.IsAllocated)
        {
            _allocHandle.Free();
        }
    }

    /// <summary>
    /// Check if texture is in acceptable LUT format
    /// </summary>
    /// <param name="texture">Texture to check</param>
    /// <param name="errorMessage">Error message describing acceptance fail reason</param>
    /// <returns></returns>
    public static bool IsTextureSupported(Texture2D texture, out string errorMessage)
    {
        try
        {
            GetChannelsForTextureFormat(texture.format);
        }
        catch (System.ArgumentException e)
        {
            errorMessage = e.Message;
            return false;
        }

        if (!ColorLutTextureConverter.TryGetTextureLayout(texture.width, texture.height, out _, out _,
                out var layoutMessage))
        {
            errorMessage = layoutMessage;
            return false;
        }

        var size = texture.width * texture.height;
        if (!IsResolutionAccepted(GetResolutionFromSize(size), size, out var resolutionMessage))
        {
            errorMessage = resolutionMessage;
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private OVRPassthroughColorLut(int size, ColorChannels channels)
    {
        Channels = channels;
        Resolution = GetResolutionFromSize(size);
        _channelCount = ChannelsToCount(channels);

        if (!IsResolutionAccepted(Resolution, size, out var message))
        {
            throw new System.ArgumentException(message);
        }

        var passthroughCapabilities = OVRManager.GetPassthroughCapabilities();
        if (passthroughCapabilities != null)
        {
            if (Resolution > passthroughCapabilities.MaxColorLutResolution)
            {
                throw new System.Exception(
                    $"Color LUT resolution {Resolution} exceeds the maximum of {passthroughCapabilities.MaxColorLutResolution}");
            }
        }
        else
        {
            Debug.LogWarning(
                "Unable to validate the maximum LUT resolution. Please instantiate OVRPassthroughColorLut after initializing the Oculus XR Plugin.");
        }
    }

    private bool IsValidUpdateResolution(int lutSize, int elementByteSize)
    {
        if (!IsInitialized)
        {
            Debug.LogError("Can not update an uninitialized lut object.");
            return false;
        }

        var resolution = GetResolutionFromSize(lutSize * elementByteSize / _channelCount);

        if (resolution != Resolution)
        {
            Debug.LogError($"Can only update with the same resolution of {Resolution}.");
            return false;
        }

        return true;
    }

    private bool IsValidLutUpdate<T>(T[] colorArray, int elementByteSize)
    {
        var arraySize = GetArraySize(colorArray);

        if (!IsValidUpdateResolution(arraySize, elementByteSize))
        {
            return false;
        }

        if (arraySize * elementByteSize != _colorBytes.Length)
        {
            Debug.LogError("New color byte array doesn't match LUT size.");
            return false;
        }

        return true;
    }

    private static ColorChannels GetChannelsForTextureFormat(TextureFormat format)
    {
        switch (format)
        {
            case TextureFormat.RGB24:
                return ColorChannels.Rgb;
            case TextureFormat.RGBA32:
                return ColorChannels.Rgba;
            default:
                throw new System.ArgumentException(
                    $"Texture format {format} not supported for Color LUTs. Supported formats are RGB24 and RGBA32.");
        }
    }

    private static int GetTextureSizeFromByteArray(byte[] initialColorLut, ColorChannels channels)
    {
        var arraySize = GetArraySize(initialColorLut);
        var channelCount = ChannelsToCount(channels);
        if (arraySize % channelCount != 0)
        {
            throw new System.ArgumentException(
                $"Invalid byte array given, {channelCount} bytes required for each color for {channels} color channels.");
        }

        return initialColorLut.Length / channelCount;
    }

    private static int GetTextureSize(Texture2D texture)
    {
        if (texture == null)
        {
            throw new System.ArgumentNullException("Lut texture is undefined.");
        }

        return texture.width * texture.height;
    }

    private static int GetArraySize<T>(T[] array)
    {
        if (array == null)
        {
            throw new System.ArgumentNullException($"Lut {typeof(T).Name} array is undefined.");
        }

        return array.Length;
    }

    private static int ChannelsToCount(ColorChannels channels)
    {
        return channels == ColorChannels.Rgb ? 3 : 4;
    }

    private static bool IsResolutionAccepted(uint resolution, int size, out string errorMessage)
    {
        if (!IsPowerOfTwo(resolution))
        {
            errorMessage = "Color LUT texture resolution should be a power of 2.";
            return false;
        }

        if (resolution * resolution * resolution != size)
        {
            errorMessage = "Unexpected LUT resolution, LUT size should be resolution in a power of 3.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool IsPowerOfTwo(uint x)
    {
        return (x != 0) && ((x & (x - 1)) == 0);
    }

    private void Create(OVRPlugin.PassthroughColorLutData lutData)
    {
        _lutData = lutData;
        IsInitialized = OVRPlugin.CreatePassthroughColorLut((OVRPlugin.PassthroughColorLutChannels)Channels,
            Resolution, _lutData, out _colorLutHandle);
        if (!IsInitialized)
        {
            Debug.LogError("Failed to create Passthrough Color LUT.");
        }
    }

    private static uint GetResolutionFromSize(int size)
    {
        return (uint)Mathf.Round(Mathf.Pow(size, 1f / 3f));
    }

    private OVRPlugin.PassthroughColorLutData CreateLutData(out byte[] colorBytes)
    {
        OVRPlugin.PassthroughColorLutData lutData = default;
        lutData.BufferSize = (uint)(Resolution * Resolution * Resolution * _channelCount);
        colorBytes = new byte[lutData.BufferSize];
        _allocHandle = GCHandle.Alloc(colorBytes, GCHandleType.Pinned);
        lutData.Buffer = _allocHandle.AddrOfPinnedObject();
        return lutData;
    }

    private OVRPlugin.PassthroughColorLutData CreateLutDataFromTexture(Texture2D lut, bool flipY)
    {
        var lutData = CreateLutData(out _colorBytes);
        ColorLutTextureConverter.TextureToColorByteMap(lut, _channelCount, _colorBytes, flipY);
        return lutData;
    }

    private OVRPlugin.PassthroughColorLutData CreateLutDataFromArray(Color[] colors)
    {
        var lutData = CreateLutData(out _colorBytes);
        WriteColorsAsBytes(colors, _colorBytes);
        return lutData;
    }

    private OVRPlugin.PassthroughColorLutData CreateLutDataFromArray(Color32[] colors)
    {
        var lutData = CreateLutData(out _colorBytes);
        WriteColorsAsBytes(colors, _colorBytes);
        return lutData;
    }

    private OVRPlugin.PassthroughColorLutData CreateLutDataFromArray(byte[] colors)
    {
        var lutData = CreateLutData(out _colorBytes);
        colors.CopyTo(_colorBytes, 0);
        return lutData;
    }

    private void WriteColorsAsBytes(Color[] colors, byte[] target)
    {
        using var source = new NativeArray<Color>(colors, Allocator.TempJob);
        using var targetNative = new NativeArray<byte>(target, Allocator.TempJob);

        var job = new WriteColorsAsBytesJob
        {
            source = source,
            target = targetNative,
            channelCount = _channelCount
        }.Schedule(source.Length, RecomendedBatchSize);

        job.Complete();
        targetNative.CopyTo(target);
    }

    private void WriteColorsAsBytes(Color32[] colors, byte[] target)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            for (int c = 0; c < _channelCount; c++)
            {
                target[i * _channelCount + c] = colors[i][c];
            }
        }
    }

    ~OVRPassthroughColorLut()
    {
        Dispose();
    }

    private void Destroy()
    {
        if (IsInitialized)
        {
            lock (_locker)
            {
                OVRPlugin.DestroyPassthroughColorLut(_colorLutHandle);
                IsInitialized = false;
            }
        }
    }

    public enum ColorChannels
    {
        Rgb = OVRPlugin.PassthroughColorLutChannels.Rgb,
        Rgba = OVRPlugin.PassthroughColorLutChannels.Rgba
    }

    private struct WriteColorsAsBytesJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        [WriteOnly]
        public NativeArray<byte> target;

        [NativeDisableParallelForRestriction]
        [ReadOnly]
        public NativeArray<Color> source;

        public int channelCount;

        public void Execute(int index)
        {
            for (int c = 0; c < channelCount; c++)
            {
                target[index * channelCount + c] = (byte)Mathf.Min(source[index][c] * 255.0f, 255.0f);
            }
        }
    }

    private static class ColorLutTextureConverter
    {
        /// <summary>
        /// Read colors from LUT texture and write them in the pre-allocated target byte array
        /// </summary>
        /// <param name="lut">Color LUT texture</param>
        /// <param name="channelCount">{RGB = 3, RGBA = 4}</param>
        /// <param name="target">Pre-allocated byte array that should fit all colors of the texture</param>
        /// <param name="flipY">Flag to inform whether the LUT texture should be flipped vertically</param>
        public static void TextureToColorByteMap(Texture2D lut, int channelCount, byte[] target, bool flipY)
        {
            MapColorValues(GetTextureSettings(lut, channelCount, flipY), lut.GetPixelData<byte>(0), target);
        }

        private static void MapColorValues(TextureSettings settings, NativeArray<byte> source, byte[] target)
        {
            using var targetNative = new NativeArray<byte>(target, Allocator.TempJob);
            var job = new MapColorValuesJob
            {
                settings = settings,
                source = source,
                target = targetNative
            }.Schedule(settings.Resolution * settings.Resolution, settings.Resolution);

            job.Complete();
            targetNative.CopyTo(target);
        }

        private static TextureSettings GetTextureSettings(Texture2D lut, int channelCount, bool flipY)
        {
            int resolution, slicesPerRow;
            if (TryGetTextureLayout(lut.width, lut.height, out resolution, out slicesPerRow, out var message))
            {
                return new TextureSettings(lut.width, lut.height, resolution, slicesPerRow, channelCount, flipY);
            }
            else
            {
                throw new System.Exception(message);
            }
        }

        // Supports 2 formats:
        // - Square, where the z (blue) planes are arranged in a square (like this https://cdn.streamshark.io/obs-guide/img/neutral-lut.png)
        //   For that, assuming that x is the edge size of the LUT, width and height must be x * sqrt(x) (-> doesn't work for all edge sizes)
        // - Horizontal, where the z (blue) planes are arranged horizontally (like this http://www.thomashourdel.com/lutify/img/tut03.jpg)
        internal static bool TryGetTextureLayout(int width, int height, out int resolution, out int slicesPerRow,
            out string errorMessage)
        {
            resolution = -1;
            slicesPerRow = -1;

            if (width == height)
            {
                float edgeLengthF = Mathf.Pow(width, 2.0f / 3.0f);
                if (Mathf.Abs(edgeLengthF - Mathf.Round(edgeLengthF)) > 0.001)
                {
                    errorMessage = "Texture layout is not compatible for color LUTs: " +
                                   "the dimensions don't result in a power-of-two resolution for the LUT. " +
                                   "Acceptable image sizes are e.g. 64 (for a LUT resolution of 16) or 512 (for a LUT resolution of 64).";
                    return false;
                }

                resolution = (int)Mathf.Round(edgeLengthF);
                slicesPerRow = (int)Mathf.Sqrt(resolution);
                Debug.Assert(width == resolution * slicesPerRow);
            }
            else
            {
                if (width != height * height)
                {
                    errorMessage = "Texture layout is not compatible for color LUTs: for horizontal layouts, " +
                                   "the Width is expected to be equal to Height * Height.";
                    return false;
                }

                resolution = height;
                slicesPerRow = resolution;
            }

            errorMessage = string.Empty;
            return true;
        }

        private struct MapColorValuesJob : IJobParallelFor
        {
            public TextureSettings settings;

            [NativeDisableParallelForRestriction]
            [WriteOnly]
            public NativeArray<byte> target;

            [NativeDisableParallelForRestriction]
            [ReadOnly]
            public NativeArray<byte> source;

            public void Execute(int index)
            {
                var bi = index / settings.Resolution;
                var gi = index % settings.Resolution;
                int bi_row = bi % settings.SlicesPerRow;
                int bi_col = (int)Mathf.Floor(bi / settings.SlicesPerRow);
                int sY = gi + bi_col * settings.Resolution;
                int y = settings.FlipY ? settings.Height - sY - 1 : sY;
                int sourceIndex = (bi_row * settings.Resolution + y * settings.Width) * settings.ChannelCount;
                int targetIndex = (bi * settings.Resolution * settings.Resolution +
                                   gi * settings.Resolution) * settings.ChannelCount;

                for (int i = 0; i < settings.Resolution * settings.ChannelCount; i++)
                {
                    target[targetIndex + i] = source[sourceIndex + i];
                }
            }
        }

        private struct TextureSettings
        {
            public int Width { get; }
            public int Height { get; }
            public int Resolution { get; }
            public int SlicesPerRow { get; }
            public int ChannelCount { get; }
            public bool FlipY { get; }

            public TextureSettings(int width, int height, int resolution, int slicesPerRow, int channelCount,
                bool flipY)
            {
                Width = width;
                Height = height;
                Resolution = resolution;
                SlicesPerRow = slicesPerRow;
                ChannelCount = channelCount;
                FlipY = flipY;
            }
        }
    }
}
