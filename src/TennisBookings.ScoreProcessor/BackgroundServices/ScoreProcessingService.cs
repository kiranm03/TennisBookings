using TennisBookings.ScoreProcessor.Logging;
using TennisBookings.ScoreProcessor.Processing;

namespace TennisBookings.ScoreProcessor.BackgroundServices;
internal class ScoreProcessingService : BackgroundService
{
	private readonly ILogger<ScoreProcessingService> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly IHostApplicationLifetime _hostApplicationLifetime;

	private readonly ISqsMessageChannel _sqsMessageChannel;

	public ScoreProcessingService(
		ILogger<ScoreProcessingService> logger,
		IServiceProvider serviceProvider,
		IHostApplicationLifetime hostApplicationLifetime,
		ISqsMessageChannel sqsMessageChannel)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
		_hostApplicationLifetime = hostApplicationLifetime;
		_sqsMessageChannel = sqsMessageChannel;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			//await Task.Delay(1000, stoppingToken); // Allow time for the channel to be populated
			//throw new InvalidOperationException("This is a test exception to ensure the service can handle errors correctly.");
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
		catch (OperationCanceledException)
		{
			_logger.OperationCancelledExceptionOccurred();
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "A critical exception was thrown in the score processing service.");
		}
		finally
		{
			_hostApplicationLifetime.StopApplication();
		}
	}
}
