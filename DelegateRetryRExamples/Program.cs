// See https://aka.ms/new-console-template for more information
using DelegateRetry;
using DelegateRetry.FluentApi;

Delegate apiCall = async (string uri) =>
{
    using (var client = new HttpClient())
    {
        var response = await client.GetAsync(uri);
        return await response.Content.ReadAsStringAsync();
    }
};

var response = await WorkRetryR
    .WillExecuteAsyncWork(apiCall)
    .WithParameters("https://www.google.com/")
    .AndRetryOn<Exception>()
    .UsingConfiguration(new DelegateRetryRConfiguration())
    .WillReturn<string>()
    .Execute();
Console.WriteLine(response);

var retryR = DelegateRetryR.Configure(config => { config.RetryConditional = (int attempt) => attempt < 4; });
var result = await retryR.RetryAsyncWorkAsync<Exception, string>(apiCall, new object[] { "https://www.google.com" });
Console.WriteLine(result);



