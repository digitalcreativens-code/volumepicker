namespace VolumeGuard.Services;

public sealed class VolumeEnforcementService : IDisposable
{
    private readonly ConfigService _config;
    private readonly ScheduleService _schedule;
    private readonly AudioService _audio;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public event Action<double, double, string>? Tick; // allowed, actual avg, slot description

    public VolumeEnforcementService(ConfigService config, ScheduleService schedule, AudioService audio)
    {
        _config = config;
        _schedule = schedule;
        _audio = audio;
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _loop = Task.Run(() => RunLoop(token), token);
    }

    public void Stop()
    {
        try
        {
            _cts?.Cancel();
            _loop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // ignore
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _loop = null;
        }
    }

    private async Task RunLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var now = TimeOnly.FromDateTime(DateTime.Now);
                var (allowed, desc) = _schedule.GetAllowedMaximum(now);
                if (_config.Current.ProtectionEnabled)
                    _audio.ClampAllEndpointsToMax(allowed);

                var actual = _audio.GetAverageMasterScalar() ?? allowed;
                var label = _config.Current.ProtectionEnabled ? desc : desc + " (zaštita isključena)";
                Tick?.Invoke(allowed, actual, label);
            }
            catch
            {
                // never crash background loop
            }

            var delay = Math.Clamp(_config.Current.PollingIntervalMs, 100, 10_000);
            try
            {
                await Task.Delay(delay, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose() => Stop();
}
