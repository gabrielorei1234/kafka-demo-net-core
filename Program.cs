﻿using Confluent.Kafka;
using Kafka.Public;
using Kafka.Public.Loggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using System.Text;
using static Confluent.Kafka.ConfigPropertyNames;

CreateHostBuilder(args).Build().Run();

static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, collection) =>
    {
        collection.AddHostedService<KafkaConsumerHostedService>();
        collection.AddHostedService<KafkaProducerHostedService>();
    });

public class KafkaConsumerHostedService : IHostedService
{
    private readonly ILogger<KafkaConsumerHostedService> _logger;
    private ClusterClient _cluster;

    public KafkaConsumerHostedService(ILogger<KafkaConsumerHostedService> logger)
    {
        _logger = logger;
        _cluster = new ClusterClient(new Configuration
        {
            Seeds = "localhost:9092"
        }, new ConsoleLogger());
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cluster.ConsumeFromLatest("demo");
        _cluster.MessageReceived += record =>
        {
            _logger.LogInformation($"Received: {Encoding.UTF8.GetString(record.Value as byte[])}");
        };

        return Task.CompletedTask;

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cluster?.Dispose();
        return Task.CompletedTask;
    }
}

public class KafkaProducerHostedService : IHostedService
{
    private readonly ILogger<KafkaProducerHostedService> _logger;
    private IProducer<Null,string> _producer;
    public KafkaProducerHostedService(ILogger<KafkaProducerHostedService> logger)
    {
        _logger = logger;
        var config = new ProducerConfig()
        {
            BootstrapServers = "localhost:9092"
        };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 100; ++i)
        {
            var value = $"Hello World {i}";
            _logger.LogInformation(value);
            await _producer.ProduceAsync("demo",new Message<Null, string>()
            {
                Value= value
            },cancellationToken);
        }

        _producer.Flush(TimeSpan.FromSeconds(10));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _producer?.Dispose();
        return Task.CompletedTask;
    }
}