using Prometheus;

namespace Chirp.Razor;

public static class ChirpMetrics
{
    public static readonly Counter CheepsCreated = Metrics.CreateCounter(
        "chirp_cheeps_created_total",
        "Total number of cheeps created");

    public static readonly Counter CommentsAdded = Metrics.CreateCounter(
        "chirp_comments_added_total",
        "Total number of comments added");
}
