namespace DynamicDnsService
{
    public class Worker : BackgroundService
    {
        private static readonly EventId eventIpChanged = new(5866, "IP changed");

        private readonly DDnsService _dnsService;
        private readonly ILogger<Worker> _logger;

        public Worker(DDnsService dnsService, ILogger<Worker> logger)
        {
            _dnsService = dnsService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _dnsService.DDnsAsync();
                    if (result != null)
                    {
                        if (result.FirstOrDefault() is ErrorInfo ev && ev.code == 0)
                        {
                            _logger.LogWarning(eventIpChanged, "Home dns has been changed. {message}", ev.message);
                        }
                        else
                        {
                            foreach (var e in result)
                            {
                                _logger.LogError(new EventId(e.code), "Error occurs, {detail}", e.message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DDns failed with {message}, at {time}", ex.Message, DateTimeOffset.Now);
                }
                await Task.Delay(TimeSpan.FromMinutes(4), stoppingToken);
            }
        }
    }
}