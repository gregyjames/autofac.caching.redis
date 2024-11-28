﻿using System.Diagnostics;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;
using gj.autofac.caching.redis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RedisCacheTester;
using Serilog;
using Serilog.Events;

static class Program
{
    static void TimeAndRun(Action action)
    {
        var start = Stopwatch.StartNew();
        action.Invoke();
        start.Stop();
        Console.WriteLine(start.Elapsed.TotalMilliseconds);
    }
    static async Task Main()
    {
        RedisConnectionManager.Host = "192.168.0.47";
        RedisConnectionManager.Port = 6379;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Verbose)
            .CreateLogger();
       
        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: false);
        });
        
        var builder = new ContainerBuilder();
       
        builder.Populate(services);
        
        builder.RegisterType<RedisCacheInterceptor>();
        
        builder.RegisterType<ExampleService>()
            .As<IExampleService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(RedisCacheInterceptor));

        var container = builder.Build();
        var service = container.Resolve<IExampleService>();

        int id = 3;

        for (int i = 0; i < 5; i++)
        {
            TimeAndRun(async () => await service.AsyncFunctionTest(id));
        }
        for (int i = 0; i < 5; i++)
        {
            TimeAndRun(async () => await service.AsyncActionTest(50));
        }
        for (int i = 0; i < 5; i++)
        {
            TimeAndRun(() => service.SynchronousTaskTest());
        }
    }
}