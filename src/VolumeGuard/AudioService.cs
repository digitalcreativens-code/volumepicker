using NAudio.CoreAudioApi;

namespace VolumeGuard;

internal static class AudioService
{
    public static float GetMasterVolume()
    {
        try
        {
            using var e = new MMDeviceEnumerator();
            foreach (var d in e.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                try { return d.AudioEndpointVolume.MasterVolumeLevelScalar; }
                catch { }
                finally { d.Dispose(); }
            }
        }
        catch { }
        return 0;
    }

    public static void ClampAll(float max)
    {
        max = Math.Clamp(max, 0f, 1f);
        try
        {
            using var e = new MMDeviceEnumerator();
            foreach (var d in e.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                try
                {
                    if (d.AudioEndpointVolume.MasterVolumeLevelScalar > max)
                        d.AudioEndpointVolume.MasterVolumeLevelScalar = max;
                }
                catch { }
                finally { d.Dispose(); }
            }
        }
        catch { }
    }
}
