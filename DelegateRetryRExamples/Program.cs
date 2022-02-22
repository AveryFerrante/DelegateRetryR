// See https://aka.ms/new-console-template for more information
using DelegateRetry;
using DelegateRetry.Tests;
using Serilog;
using Serilog.Extensions.Logging;

var logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();
var retryRLogger = new SerilogLoggerFactory(logger)
    .CreateLogger("DelegateRetryR");

// Construction using static Configure function (can also instantiate / use DI)
var retryR = DelegateRetryR.ConfigureWithLogger(config =>
{
    config.RetryConditional = (retryAttempt) => retryAttempt < 3;
    config.RetryDelay = (retryAttempt) => retryAttempt * 300;
}, retryRLogger);
await SimpleSyncJobExample(retryR);
await AsyncWebcallExample(retryR);
await FailingJobExample(retryR);


var writeFileJob = (string path, string content) => File.WriteAllTextAsync(path, content);
var getGoogleContentJob = async () =>
{
    using (var client = new HttpClient())
    {
        var response = await client.GetAsync("http://www.google.com");
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }
};
var retryRGetGoogleContentJob = retryR.RetryAsyncWorkAsync<Exception, string>(getGoogleContentJob, null);
var taskList = new List<Task>();
for (int i = 0; i < 10; i++)
{
    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestFolder") + $"/file{i + 1}.txt";
    taskList.Add(retryR.RetryAsyncWorkAsync<Exception>(writeFileJob, parameters: new object[] { path, await retryRGetGoogleContentJob }));
}
Task.WaitAll(taskList.ToArray());









async static Task SimpleSyncJobExample(IDelegateRetryR retryR)
{
    // Running a simple syncronous delegate
    Console.WriteLine("Running a simple sync job that returns a value");
    var simpleSyncJob = ((int a) => a + 10);
    // Retries on any Exception thrown & will return an integer
    var result = await retryR.RetryWorkAsync<Exception, int>(simpleSyncJob, parameters: new object[] { 10 });
    Console.WriteLine($"Result of simple sync job: {result}\n\n");
}

async static Task AsyncWebcallExample(IDelegateRetryR retryR)
{
    // Running a realisitc & more complex async delegate with a response
    // Notice it will "unfurl" the inner async job & return the final result
    var url = "http://www.google.com";
    Console.WriteLine($"Making a web call to {url}");
    var getContentLengthJob = (async () =>
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            return content.Length;
        }
    });
    var contentLength = await retryR.RetryAsyncWorkAsync<HttpRequestException, int>(getContentLengthJob, parameters: null);
    Console.WriteLine($"Length of {url}: {contentLength}\n\n");
}

async static Task FailingJobExample(IDelegateRetryR retryR)
{
    // FailingDelegateBuilder is a class from the assocaited testing library. It sets up a delegate that will fail with a specific
    // error a specific number of times
    var timesToFail = 2;
    Console.WriteLine($"Running a job that will fail {timesToFail} time(s)");
    var jobThatFailsTwoTimes = FailingDelegateBuilder
        .WillThrow(new InvalidCastException())
        .WithFailureCount(timesToFail)
        .PerformsWork((int input) => Task.Run(() => { Thread.Sleep(1000); return input; }))
        .BuildWithExpectedParamsAndReturnType<int, Task<int>>();
    var result = await retryR.RetryAsyncWorkAsync<InvalidCastException, int>(jobThatFailsTwoTimes, parameters: new object[] { 500 });
    Console.WriteLine($"Final result of job: {result}\n\n");
}

