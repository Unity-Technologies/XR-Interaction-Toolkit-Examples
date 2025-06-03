// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_MATHEMATICS

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Meta.Utilities
{
    public static class MathUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Wrap(int value, int length)
        {
            var r = value % length;
            return r < 0 ? r + length : r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Square(float value) => value * value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Square(int value) => value * value;

#pragma warning disable IDE1006 // ReSharper disable InconsistentNaming

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 czero() => 0.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 creal(float r) => float2(r, 0.0f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cimg(float i) => float2(0.0f, i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cadd(float2 c0, float2 c1) => c0 + c1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 csub(float2 c0, float2 c1) => c0 - c1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cmul(float2 c0, float2 c1) => float2(c0.x * c1.x - c0.y * c1.y, c0.x * c1.y + c0.y * c1.x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 conj(float2 c) => float2(c.x, -c.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cexp(float2 c) => float2(cos(c.y) * exp(c.x), sin(c.y) * exp(c.x));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 cdot(float2 c0, float2 c1) => float2(c0.x * c1.x + c0.y * c1.y, c0.y * c1.x - c0.x * c1.y);

#pragma warning restore IDE1006 // ReSharper restore InconsistentNaming

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Snap(float value, float cellSize) => MathF.Floor(value / cellSize) * cellSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Map(this float value, float inputMin, float inputMax, float outputMin, float outputMax) => (value - inputMin) / (inputMax - inputMin) * (outputMax - outputMin) + outputMin;

        //TODO: Ensure functionality for non-clamped map is not being used and merge with clamped map
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ClampedMap(this float value, float inputMin, float inputMax, float outputMin, float outputMax) => clamp(value, inputMin, inputMax).Map(inputMin, inputMax, outputMin, outputMax);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Atanh(float x) => 0.5f * log((1.0f + x) / (1.0f - x));

        /// <summary>
        /// Maintains a rolling average of a float value, expressed in units per second.
        /// </summary>
        public class RollingAverage
        {
            private Queue<float> m_sampleValueQueue;
            private Queue<float> m_sampleTimeQueue;
            private float m_rollingValueSum;
            private float m_rollingTimeSum;

            public float RollingValueMean { get; private set; }
            public float RollingTimeMean { get; private set; }
            public float RollingMeanPerSecond { get; private set; }

            public float RequiredSamples { get; }
            public int MaxSampleCount { get; }

            public RollingAverage(int maxSamples = 60, float requiredSamples = 0.2f)
            {
                MaxSampleCount = maxSamples;
                RequiredSamples = requiredSamples;
                m_sampleTimeQueue = new Queue<float>(MaxSampleCount);
                m_sampleValueQueue = new Queue<float>(MaxSampleCount);
                Reset();
            }

            public void Reset()
            {
                m_sampleTimeQueue.Clear();
                m_sampleValueQueue.Clear();
                m_rollingValueSum = 0;
                m_rollingTimeSum = 0;
                RollingValueMean = 0;
                RollingTimeMean = 0;
                RollingMeanPerSecond = 0;
            }

            public float AddSample(float value, float time)
            {
                if (m_sampleValueQueue.Count == MaxSampleCount && m_sampleValueQueue.TryDequeue(out var oldSampleValue) && m_sampleTimeQueue.TryDequeue(out var oldSampleTime))
                {
                    m_rollingValueSum -= oldSampleValue;
                    m_rollingTimeSum -= oldSampleTime;
                }

                m_rollingValueSum += value;
                m_rollingTimeSum += time;

                m_sampleValueQueue.Enqueue(value);
                m_sampleTimeQueue.Enqueue(time);

                RollingValueMean = m_rollingValueSum / m_sampleValueQueue.Count;
                RollingTimeMean = m_rollingTimeSum / m_sampleTimeQueue.Count;

                RollingMeanPerSecond = m_sampleValueQueue.Count < MaxSampleCount * RequiredSamples ? 0f
                    : RollingTimeMean <= 0 ? 0 : RollingValueMean / RollingTimeMean;

                return RollingMeanPerSecond;
            }
        }
    }
}

#endif