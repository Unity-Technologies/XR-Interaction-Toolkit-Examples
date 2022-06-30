using UnityEngine;
using System.Collections;
using System;
using System.IO;

namespace DigitalOpus.MB.Core
{
public static class MB_TGAWriter
{
    public static void Write(Color[] pixels, int width, int height, string path)
    {
        // Delete the file if it exists.
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        //Create the file.
        FileStream fs = File.Create(path);
        Write(pixels, width, height, fs);
    }


    public static void Write(Color[] pixels, int width, int height, Stream output)
    {
        byte[] pixelsArr = new byte[pixels.Length * 4];

        int offsetSource = 0;
        int offsetDest = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color value = pixels[offsetSource];
                pixelsArr[offsetDest] = (byte)(value.b * 255); // b
                pixelsArr[offsetDest + 1] = (byte)(value.g * 255); // g
                pixelsArr[offsetDest + 2] = (byte)(value.r * 255); // r
                pixelsArr[offsetDest + 3] = (byte)(value.a * 255); // a

                offsetSource++;
                offsetDest += 4;
            }
        }

        byte[] header = new byte[] {
            0, // ID length
            0, // no color map
            2, // uncompressed, true color
            0, 0, 0, 0,
            0,
            0, 0, 0, 0, // x and y origin
            (byte)(width & 0x00FF),
            (byte)((width & 0xFF00) >> 8),
            (byte)(height & 0x00FF),
            (byte)((height & 0xFF00) >> 8),
            32, // 32 bit bitmap
            0 };

        using (BinaryWriter writer = new BinaryWriter(output))
        {
            writer.Write(header);
            writer.Write(pixelsArr);
            }
        }
    }
}
