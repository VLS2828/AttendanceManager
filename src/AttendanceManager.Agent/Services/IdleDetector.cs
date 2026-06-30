using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace AttendanceManager.Agent.Services;

public class IdleDetector
{
    private readonly ILogger<IdleDetector> _logger;
    private readonly int _idleThresholdMinutes;
    private DateTime? _idleStartTime;
    private bool _isIdle;
    private readonly System.Timers.Timer _checkTimer;

    public event EventHandler<IdleEventArgs>? IdlePeriodDetected;

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    public IdleDetector(ILogger<IdleDetector> logger, int idleThresholdMinutes = 6)
    {
        _logger = logger;
        _idleThresholdMinutes = idleThresholdMinutes;

        _checkTimer = new System.Timers.Timer(30_000); // check every 30 seconds
        _checkTimer.Elapsed += CheckIdleStatus;
        _checkTimer.AutoReset = true;
    }

    public void Start()
    {
        _checkTimer.Start();
        _logger.LogInformation("Idle detector started with {Threshold}-minute threshold", _idleThresholdMinutes);
    }

    public void Stop()
    {
        _checkTimer.Stop();
        if (_isIdle && _idleStartTime.HasValue)
        {
            RaiseIdleEvent(_idleStartTime.Value, DateTime.Now);
        }
        _logger.LogInformation("Idle detector stopped");
    }

    private void CheckIdleStatus(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            var idleMinutes = GetIdleTimeMinutes();

            if (idleMinutes >= _idleThresholdMinutes)
            {
                if (!_isIdle)
                {
                    _idleStartTime = DateTime.Now.AddMinutes(-idleMinutes);
                    _isIdle = true;
                    _logger.LogInformation("Idle period started at {Time}", _idleStartTime);
                }
            }
            else
            {
                if (_isIdle && _idleStartTime.HasValue)
                {
                    var idleEndTime = DateTime.Now.AddMinutes(-idleMinutes);
                    RaiseIdleEvent(_idleStartTime.Value, idleEndTime);
                    _isIdle = false;
                    _idleStartTime = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking idle status");
        }
    }

    private static double GetIdleTimeMinutes()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return 0;

        var lastInput = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref lastInput))
            return 0;

        var idleMs = (uint)Environment.TickCount - lastInput.dwTime;
        return idleMs / 60_000.0;
    }

    private void RaiseIdleEvent(DateTime start, DateTime end)
    {
        var duration = (end - start).TotalMinutes;
        if (duration >= _idleThresholdMinutes)
        {
            _logger.LogInformation("Idle period detected: {Start} to {End} ({Duration:F1} min)", start, end, duration);
            IdlePeriodDetected?.Invoke(this, new IdleEventArgs(start, end, duration));
        }
    }
}

public class IdleEventArgs : EventArgs
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public double DurationMinutes { get; }

    public IdleEventArgs(DateTime startTime, DateTime endTime, double durationMinutes)
    {
        StartTime = startTime;
        EndTime = endTime;
        DurationMinutes = durationMinutes;
    }
}
