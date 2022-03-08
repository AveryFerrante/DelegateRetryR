// See https://aka.ms/new-console-template for more information
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
    .UsingDefaultConfiguration()
    .WillReturn<string>()
    .Execute();
Console.WriteLine(response);



