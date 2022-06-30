/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Facebook.WitAi.Data
{
    public class RingBuffer<T>
    {
        public delegate void OnDataAdded(T[] data, int offset, int length);

        public OnDataAdded OnDataAddedEvent;

        private T[] buffer;
        private int bufferIndex;
        private long bufferDataLength;
        public int Capacity => buffer.Length;

        public void Clear(bool eraseData = false)
        {
            bufferIndex = 0;
            bufferDataLength = 0;

            if (eraseData)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = default(T);
                }
            }
        }

        public class Marker
        {
            public long bufferDataIndex;
            public int index;
            public RingBuffer<T> buffer;

            public bool IsValid => buffer.bufferDataLength - bufferDataIndex < buffer.Capacity;

            public int Read(T[] buffer, int offset, int length)
            {
                int read = -1;
                if (IsValid)
                {
                    read = this.buffer.Read(buffer, offset, length, bufferDataIndex);
                    bufferDataIndex += read;
                    index += read;
                    if (index > buffer.Length) index -= buffer.Length;
                }


                return read;
            }
        }

        public RingBuffer(int capacity)
        {
            buffer = new T[capacity];
        }

        private int CopyToBuffer(T[] data, int offset, int length, int bufferIndex)
        {
            if (length > buffer.Length)
                throw new ArgumentException(
                    "Push data exceeds buffer size.");

            if (bufferIndex + length < buffer.Length)
            {
                Array.Copy(data, offset, buffer, bufferIndex, length);
                return bufferIndex + length;
            }
            else
            {
                int len = Mathf.Min(length, buffer.Length);
                int endChunkLength = buffer.Length - bufferIndex;
                int wrappedChunkLength = len - endChunkLength;
                try
                {

                    Array.Copy(data, offset, buffer, bufferIndex, endChunkLength);
                    Array.Copy(data, offset + endChunkLength, buffer, 0, wrappedChunkLength);
                    return wrappedChunkLength;
                }
                catch (ArgumentException e)
                {
                    throw e;
                }
            }
        }

        private int CopyFromBuffer(T[] data, int offset, int length, int bufferIndex)
        {
            if (length > buffer.Length)
                throw new ArgumentException(
                    $"Push data exceeds buffer size {length} < {buffer.Length}" );

            if (bufferIndex + length < buffer.Length)
            {
                Array.Copy(buffer, bufferIndex, data, offset, length);
                return bufferIndex + length;
            }
            else
            {
                var l = Mathf.Min(buffer.Length, length);
                int endChunkLength = buffer.Length - bufferIndex;
                int wrappedChunkLength = l - endChunkLength;

                Array.Copy(buffer, bufferIndex, data, offset, endChunkLength);
                Array.Copy(buffer, 0, data, offset + endChunkLength, wrappedChunkLength);
                return wrappedChunkLength;
            }
        }

        public void Push(T[] data, int offset, int length)
        {
            lock (buffer)
            {
                bufferIndex = CopyToBuffer(data, offset, length, bufferIndex);
                bufferDataLength += length;
                OnDataAddedEvent?.Invoke(data, offset, length);
            }
        }

        public int Read(T[] data, int offset, int length, long bufferDataIndex)
        {
            lock (buffer)
            {
                int read = (int) (Math.Min(bufferDataIndex + length, bufferDataLength) -
                                  bufferDataIndex);

                int bufferIndex = this.bufferIndex - (int) (bufferDataLength - bufferDataIndex);
                if (bufferIndex < 0)
                {
                    bufferIndex = buffer.Length + bufferIndex;
                }

                CopyFromBuffer(data, offset, length, bufferIndex);

                return read;
            }
        }

        public Marker CreateMarker(int offset = 0)
        {
            var markerPosition = bufferDataLength + offset;
            if (markerPosition < 0)
            {
                markerPosition = 0;
            }

            Debug.Log("Creating marker at " + bufferDataLength + " offset to " + markerPosition);

            int bufIndex = bufferIndex + offset;
            if (bufIndex < 0)
            {
                bufIndex = buffer.Length + bufIndex;
            }

            if (bufIndex > buffer.Length)
            {
                bufIndex = bufIndex - buffer.Length;
            }

            var marker = new Marker()
            {
                buffer = this,
                bufferDataIndex = markerPosition,
                index = bufIndex
            };
            return marker;
        }
    }
}
