using System.Diagnostics;
namespace Timerki;
public sealed class TimerModel
{
    private readonly Stopwatch _clock = new();
    private TimeSpan _duration;
    public bool Active { get; private set; }
    public bool Done { get; private set; }
    public bool Running => Active && !Done;
    public TimerModel(int seconds) => _duration = TimeSpan.FromSeconds(seconds);
    public void Start(int seconds)
    {
        _duration = TimeSpan.FromSeconds(seconds);
        _clock.Restart();
        Active = true;
        Done = false;
    }
    public void Stop()
    {
        _clock.Reset();
        Active = false;
        Done = false;
    }
    // True only once at the moment the timer finishes.
    public bool Update()
    {
        if (!Running || _clock.Elapsed < _duration) return false;
        _clock.Stop();
        Done = true;
        return true;
    }
    public string Display
    {
        get
        {
            if (!Active || Done) return "00:00";
            var remaining = _duration - _clock.Elapsed;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            int seconds = (int)Math.Ceiling(remaining.TotalSeconds);
            return $"{seconds/60:00}:{seconds%60:00}";
        }
    }
}
