namespace TennisBookings.ScoreProcessor.BackgroundServices;
internal class ScoreProcessingService : BackgroundService
{
	private readonly ILogger<ScoreProcessingService> _logger;
	private readonly IServiceProvider _serviceProvider;

	private readonly ISqsMessageChannel _sqsMessageChannel;

	public ScoreProcessingService(
		ILogger<ScoreProcessingService> logger,
		IServiceProvider serviceProvider,
		ISqsMessageChannel sqsMessageChannel)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
		_sqsMessageChannel = sqsMessageChannel;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Score Processing Service is starting.");
		while (!stoppingToken.IsCancellationRequested)
		{
			using (var scope = _serviceScopeFactory.CreateScope())
			{
				// Here you would typically resolve your services and process scores.
				// For example:
				// var scoreProcessor = scope.ServiceProvider.GetRequiredService<IScoreProcessor>();
				// await scoreProcessor.ProcessScoresAsync(stoppingToken);
			}
			await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
		}
		_logger.LogInformation("Score Processing Service is stopping.");
	}
}
{
}
