using System.Collections.Generic;
using UnityEngine;

public static class NoiseManager
{
    private const float NoiseLifetime = 1.25f;
    private static readonly List<NoiseSource> NoiseSources = new();

    public static void EmitNoise(Vector3 position, float intensity)
    {
        if (intensity <= 0f)
        {
            return;
        }

        NoiseSources.Add(new NoiseSource(position, intensity, Time.time));
        RemoveExpiredNoise();
    }

    public static bool TryGetClosestNoise(Vector3 listenerPosition, float hearingRange, out NoiseSource closestNoise)
    {
        RemoveExpiredNoise();

        closestNoise = default;
        var closestScore = float.PositiveInfinity;
        var found = false;

        foreach (var source in NoiseSources)
        {
            var effectiveRange = hearingRange * Mathf.Clamp01(source.Intensity);
            var distance = Vector3.Distance(listenerPosition, source.Position);

            if (distance > effectiveRange || distance >= closestScore)
            {
                continue;
            }

            closestNoise = source;
            closestScore = distance;
            found = true;
        }

        return found;
    }

    private static void RemoveExpiredNoise()
    {
        NoiseSources.RemoveAll(source => Time.time - source.Timestamp > NoiseLifetime);
    }

    public readonly struct NoiseSource
    {
        public NoiseSource(Vector3 position, float intensity, float timestamp)
        {
            Position = position;
            Intensity = intensity;
            Timestamp = timestamp;
        }

        public Vector3 Position { get; }
        public float Intensity { get; }
        public float Timestamp { get; }
    }
}
