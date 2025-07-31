using System.Net;
using Amazon.SQS.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TennisBookings.ScoreProcessor.BackgroundServices;
using TennisBookings.ScoreProcessor.Sqs;

namespace TennisBookings.ScoreProcessor.Tests;
public class QueueReadingServiceTests
{
	[Fact]
	public async Task ShouldSwallowExceptions_AndCompleteWriter()
	{
		// Arrange
		var sqsChannel = new Mock<ISqsMessageChannel>();
		var sqsMessageQueue = new Mock<ISqsMessageQueue>();
		sqsMessageQueue
			.Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new Exception("Test exception"));

		using var sut = new QueueReadingService(
			NullLogger<QueueReadingService>.Instance,
			Options.Create(new AwsServicesConfiguration
			{
				UseLocalStack = true,
				LocalstackScoresQueueUrl = "http://localhost:4566/000000000000/scores-queue"
			}),
			sqsMessageQueue.Object,
			sqsChannel.Object);

		// Act
		await sut.StartAsync(default);

		// Assert
		sqsChannel.Verify(x => x.TryCompleteWriter(null), Times.Once);
	}

	[Fact]
	public async Task ShouldStopWithoutException_WhenCancelled()
	{
		// Arrange
		var sqsChannel = new SqsMessageChannel(NullLogger<SqsMessageChannel>.Instance);
		var sqsMessageQueue = new Mock<ISqsMessageQueue>();
		sqsMessageQueue
			.Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ReceiveMessageResponse
			{
				HttpStatusCode = HttpStatusCode.OK,
				Messages = new List<Message>()
			});
		using var sut = new QueueReadingService(
			NullLogger<QueueReadingService>.Instance,
			Options.Create(new AwsServicesConfiguration
			{
				UseLocalStack = true,
				LocalstackScoresQueueUrl = "http://localhost:4566/000000000000/scores-queue"
			}),
			sqsMessageQueue.Object,
			sqsChannel);
		// Act
		await sut.StartAsync(default);

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
		Func<Task> act = async () => await sut.StopAsync(cts.Token);
		// Assert
		await act.Should().NotThrowAsync();
	}
}
