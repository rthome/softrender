﻿using SharpDX;
using System;

namespace SoftRender.Engine
{
    static class MathExtensions
    {
        public static float Clamp(this float value, float min = 0, float max = 1)
        {
            return Math.Max(min, Math.Min(value, max));
        }

        // Compute the cosine of the angle between the light vector and the normal vector
        // Returns a value between 0 and 1
        public static float ComputeNDotL(ref Vector3 vertex, ref Vector3 normal, ref Vector3 lightPosition)
        {
            var lightDirection = Vector3.Normalize(lightPosition - vertex);
            return Math.Max(0, Vector3.Dot(normal, lightDirection));
        }

        public static float Interpolate(this float t, float a, float b)
        {
            return (1 - t) * a + b * t;
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }
    }
}
