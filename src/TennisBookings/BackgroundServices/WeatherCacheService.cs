using Microsoft.Extensions.Options;
using TennisBookings.External;

namespace TennisBookings.BackgroundServices;

public class WeatherCacheService : BackgroundService
{
    private readonly IDistributedCache<WeatherResult> _cache;
    private readonly IWeatherApiClient _weatherApiClient;
    private readonly IOptionsMonitor<ExternalServicesConfiguration> _options;
    private readonly ILogger<WeatherCacheService> _logger;
    private readonly int _minutesToCache;
    private readonly int _refreshIntervalInSeconds;

    public WeatherCacheService(
        IDistributedCache<WeatherResult> cache,
        IWeatherApiClient weatherApiClient,
        IOptionsMonitor<ExternalServicesConfiguration> options,
        ILogger<WeatherCacheService> logger)
    {
        _cache = cache;
        _weatherApiClient = weatherApiClient;
        _options = options;
        _logger = logger;
        _minutesToCache = options.Get(ExternalServicesConfiguration.WeatherApi).MinsToCache;
        _refreshIntervalInSeconds = _minutesToCache > 1
            ? (_minutesToCache - 1) * 60
            : 30; // Default to 30 seconds if less than 1 minute
    }

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			var forecast = await _weatherApiClient.GetWeatherForecastAsync("Melbourne", stoppingToken);

			if (forecast is not null)
			{
				var currentWeather = new WeatherResult
				{
					City = "Melbourne",
					Weather = forecast.Weather
				};
				var cacheKey = $"current_weather_{DateTime.UtcNow:yyyy_MM_dd}";

				_logger.LogInformation("Updating weather in cache");

				await _cache.SetAsync(cacheKey, currentWeather, _minutesToCache);
			}

			await Task.Delay(TimeSpan.FromSeconds(_refreshIntervalInSeconds), stoppingToken);
		}
	}
}
