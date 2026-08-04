namespace AuxiliumSoftware.AuxiliumServices.API.Metrics
{
    public sealed class HttpMetricsAccumulator
    {
        private const int ReservoirSize = 4096;

        private long _requests, _server5xx, _exceptions;
        private readonly Lock _lock = new();
        private readonly double[] _reservoir = new double[ReservoirSize];
        private int _filled;
        private long _seen;
        private readonly Random _rng = new();

        public void Record(double elapsedMs, int statusCode, bool threw)
        {
            Interlocked.Increment(ref _requests);
            if (statusCode >= 500) Interlocked.Increment(ref _server5xx);
            if (threw) Interlocked.Increment(ref _exceptions);

            lock (_lock)
            {
                var n = _seen++;
                if (_filled < ReservoirSize) _reservoir[_filled++] = elapsedMs;
                else
                {
                    var j = (long)(_rng.NextDouble() * (n + 1));
                    if (j < ReservoirSize)
                    {
                        _reservoir[(int)j] = elapsedMs;
                    }
                }
            }
        }

        public HttpWindow Drain()
        {
            long req = Interlocked.Exchange(ref _requests, 0);
            long s5 = Interlocked.Exchange(ref _server5xx, 0);
            long ex = Interlocked.Exchange(ref _exceptions, 0);

            double[] lat;
            lock (_lock)
            {
                lat = _reservoir.AsSpan(0, _filled).ToArray();
                _filled = 0;
                _seen = 0;
            }
            Array.Sort(lat);

            double P(double q) => lat.Length == 0
                ? 0
                : lat[Math.Clamp((int)Math.Ceiling(q * lat.Length) - 1, 0, lat.Length - 1)];

            return new HttpWindow(req, s5, ex, P(0.50), P(0.95), P(0.99));
        }
    }
}
