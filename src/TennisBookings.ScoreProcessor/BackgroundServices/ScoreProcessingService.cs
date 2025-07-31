using TennisBookings.ScoreProcessor.Processing;

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
		await foreach (var message in _sqsMessageChannel.Reader.ReadAllAsync()
			.WithCancellation(stoppingToken))
		{
			_logger.LogInformation("Read message {MessageId} from channel.", message.MessageId);

			using var scope = _serviceProvider.CreateScope();
			var scoreProcessor = scope.ServiceProvider.GetRequiredService<IScoreProcessor>();

			await scoreProcessor.ProcessScoresFromMessageAsync(message, stoppingToken);

			_logger.LogInformation("Finished processing message {MessageId} from channel.", message.MessageId);
		}

		_logger.LogInformation("Score processing service has finished processing all available messages from the channel.");
	}
}
