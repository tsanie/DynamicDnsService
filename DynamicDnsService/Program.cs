using DynamicDnsService;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "DynamicDnsService";
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddHttpClient<DDnsService>(httpClient =>
        {
            httpClient.DefaultRequestHeaders.Add("X-Auth-Email", "test@example.com");
            httpClient.DefaultRequestHeaders.Add("X-Auth-Key", "");
        });
    })
    .Build();

await host.RunAsync();
