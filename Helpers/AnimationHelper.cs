namespace IbrahimAbdo.Login.Helpers;

internal sealed class AnimationHelper : IDisposable
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
    private DateTime _startTime;
    private int _durationMs;
    private float _from;
    private float _to;
    private Action<float>? _onUpdate;
    private Action? _onComplete;

    public AnimationHelper()
    {
        _timer.Tick += OnTick;
    }

    public void Animate(float from, float to, int durationMs, Action<float> onUpdate, Action? onComplete = null)
    {
        _from = from;
        _to = to;
        _durationMs = Math.Max(1, durationMs);
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        _startTime = DateTime.UtcNow;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var elapsed = (DateTime.UtcNow - _startTime).TotalMilliseconds;
        var progress = Math.Clamp((float)(elapsed / _durationMs), 0F, 1F);
        var eased = EaseOutCubic(progress);
        var value = _from + ((_to - _from) * eased);

        _onUpdate?.Invoke(value);

        if (progress >= 1F)
        {
            _timer.Stop();
            _onComplete?.Invoke();
        }
    }

    private static float EaseOutCubic(float t) => 1F - MathF.Pow(1F - t, 3F);

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
