// See https://aka.ms/new-console-template for more information
using DelegateRetry;
using DelegateRetry.FluentApi;


var syncWork = (() => 10);


var result = await WorkRetryR
    .WillExecuteAsyncWork(DoWork)
    .WithParameters(new HttpClient(), 500)
    .AndRetryOn<Exception>()
    .UsingConfiguration(new DelegateRetryRConfiguration { RetryConditional = (int attempts) => attempts < 3 })
    .WillReturn<string>()
    .Execute();

Console.WriteLine(result);


static async Task<string> DoWork(HttpClient client, int randomNumber)
{
    Console.WriteLine($"RANDOM NUMB: {randomNumber}");
    Thread.Sleep(4000);
    var result = await client.GetAsync("https://www.google.com");
    return await result.Content.ReadAsStringAsync();
}



