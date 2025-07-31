global using System.Text.Json;
global using Amazon.S3;
global using Amazon.S3.Model;
global using Amazon.SQS;
global using Amazon.SQS.Model;
global using Microsoft.Extensions.Options;
global using TennisBookings.ResultsProcessing;
global using TennisBookings.ScoreProcessor;
global using TennisBookings.ScoreProcessor.S3;
global using TennisBookings.ScoreProcessor.Sqs;
using Amazon.Runtime.CredentialManagement;
using TennisBookings.ScoreProcessor.BackgroundServices;

var host = Host.CreateDefaultBuilder(args)
	.ConfigureServices((hostContext, services) =>
	{
		services.Configure<HostOptions>(hostOptions =>
		{
			hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
			hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(60);
		});

		services.Configure<AwsServicesConfiguration>(hostContext.Configuration.GetSection("AWS"));

		//var chain = new CredentialProfileStoreChain();

		//if (!chain.TryGetAWSCredentials("default", out var credentials))
		//	throw new Exception("❌ Could not load AWS credentials from profile");

		//services.AddSingleton<IAmazonSQS>(
		//	new AmazonSQSClient(credentials, Amazon.RegionEndpoint.APSoutheast2));

		services.AddAWSService<IAmazonS3>();
		services.AddAWSService<IAmazonSQS>();

		var useLocalStack = hostContext.Configuration.GetValue<bool>("AWS:UseLocalStack");

		if (hostContext.HostingEnvironment.IsDevelopment() && useLocalStack)
		{
			services.AddSingleton<IAmazonSQS>(sp =>
			{
				var s3Client = new AmazonSQSClient(new AmazonSQSConfig
				{
					ServiceURL = "http://localhost:4566",
					AuthenticationRegion = hostContext.Configuration.GetValue<string>("AWS:Region") ?? "eu-west-2"
				});

				return s3Client;
			});

			services.AddSingleton<IAmazonS3>(sp =>
			{
				var s3Client = new AmazonS3Client(new AmazonS3Config
				{
					ServiceURL = "http://localhost:4566",
					ForcePathStyle = true,
					AuthenticationRegion = hostContext.Configuration.GetValue<string>("AWS:Region") ?? "eu-west-2"
				});

				return s3Client;
			});
		}

		services.AddSingleton<ISqsMessageChannel, SqsMessageChannel>();
		services.AddSingleton<ISqsMessageDeleter, SqsMessageDeleter>();
		services.AddSingleton<ISqsMessageQueue, SqsMessageQueue>();

		services.AddHostedService<QueueReadingService>();
		services.AddHostedService<ScoreProcessingService>();
	})
	.Build();

await host.RunAsync();
