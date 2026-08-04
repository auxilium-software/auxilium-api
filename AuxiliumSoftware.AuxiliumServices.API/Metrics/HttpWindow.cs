namespace AuxiliumSoftware.AuxiliumServices.API.Metrics
{
    public readonly record struct HttpWindow(
        long Requests,
        long Server5xx,
        long Exceptions,
        double P50,
        double P95,
        double P99
    );
}
