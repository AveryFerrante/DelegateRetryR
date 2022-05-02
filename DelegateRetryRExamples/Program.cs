// See https://aka.ms/new-console-template for more information
using DelegateRetry;
using DelegateRetry.FluentApi;
using DelegateRetry.LogAdapter.Serilog;
using Serilog.Events;
using Serilog;

Delegate apiCall = async (string uri) =>
{
    using (var client = new HttpClient())
    {
        var response = await client.GetAsync(uri);
        throw new Exception();
        return await response.Content.ReadAsStringAsync();
    }
};

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Error()
    .MinimumLevel.Override("DelegateRetry", LogEventLevel.Debug)
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} ThreadId:{ThreadId}{NewLine}{Exception}")
    .CreateLogger();
DelegateRetryR.UseLogger<SerilogAdapter>();

var result = await WorkRetryR
    .WillExecuteAsyncWork(apiCall)
    .WithParameters("https://www.google.com")
    .AndRetryOn<Exception>()
    .UsingDefaultConfiguration()
    .WillReturn<string>()
    .Execute();

Console.WriteLine(result);




