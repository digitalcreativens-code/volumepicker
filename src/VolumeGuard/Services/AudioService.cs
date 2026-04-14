using NAudio.CoreAudioApi;

namespace VolumeGuard.Services;

/// <summary>
/// Native master volume control via Core Audio (NAudio). Applies to each active playback endpoint.
/// </summary>
public sealed class AudioService : IDisposable
{
    private readonly object _gate = new();

    public IReadOnlyList<string> GetActivePlaybackDeviceNames()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var list = enumerator
                .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .Select(d => d.FriendlyName)
                .ToList();
            return list;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>Returns average master scalar across active playback devices, or null if unavailable.</summary>
    public double? GetAverageMasterScalar()
    {
        lock (_gate)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();
                if (devices.Count == 0) return null;

                var sum = 0.0;
                foreach (var d in devices)
                {
                    try
                    {
                        sum += d.AudioEndpointVolume.MasterVolumeLevelScalar;
                    }
                    catch
                    {
                        // skip device
                    }
                    finally
                    {
                        d.Dispose();
                    }
                }

                return sum / devices.Count;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>Clamp every active playback endpoint master volume to at most <paramref name="maxScalar"/>.</summary>
    public void ClampAllEndpointsToMax(double maxScalar)
    {
        maxScalar = Math.Clamp(maxScalar, 0, 1);
        lock (_gate)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();
                foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
                {
                    try
                    {
                        var vol = device.AudioEndpointVolume;
                        var cur = vol.MasterVolumeLevelScalar;
                        if (cur > maxScalar)
                            vol.MasterVolumeLevelScalar = (float)maxScalar;
                    }
                    catch
                    {
                        // device may be disconnected mid-loop
                    }
                    finally
                    {
                        device.Dispose();
                    }
                }
            }
            catch
            {
                // enumerator failure — ignore this tick
            }
        }
    }

    public void Dispose()
    {
    }
}
