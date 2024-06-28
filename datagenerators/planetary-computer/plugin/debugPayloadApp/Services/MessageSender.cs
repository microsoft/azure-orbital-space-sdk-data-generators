using Microsoft.Azure.SpaceFx.MessageFormats.Common;
using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor;

namespace DebugClient;

public class MessageSender : BackgroundService {
    private readonly ILogger<MessageSender> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Core.Client _client;
    private readonly string _appId;
    private readonly string _hostSvcAppId;
    private readonly List<string> _appsOnline = new();
    private readonly TimeSpan MAX_TIMESPAN_TO_WAIT_FOR_MSG = TimeSpan.FromSeconds(10);

    public MessageSender(ILogger<MessageSender> logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _client = _serviceProvider.GetService<Core.Client>() ?? throw new NullReferenceException($"{nameof(Core.Client)} is null");
        _appId = _client.GetAppID().Result;
        _hostSvcAppId = _appId.Replace("-client", "");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // This is higher than normal to let the service build the datagenerator if it needs to
        DateTime maxTimeToWait = DateTime.Now.Add(TimeSpan.FromSeconds(90));

        using (var scope = _serviceProvider.CreateScope()) {
            _logger.LogInformation("MessageSender running at: {time}", DateTimeOffset.Now);

            Boolean SVC_ONLINE = _client.ServicesOnline().Any(pulse => pulse.AppId.Equals(_hostSvcAppId, StringComparison.CurrentCultureIgnoreCase));

            _logger.LogInformation($"Waiting for service '{_hostSvcAppId}' to come online...");

            while (!SVC_ONLINE && DateTime.Now < maxTimeToWait) {
                await Task.Delay(1000);
                SVC_ONLINE = _client.ServicesOnline().Any(pulse => pulse.AppId.Equals(_hostSvcAppId, StringComparison.CurrentCultureIgnoreCase));
                ListHeardServices();
            }

            if (!SVC_ONLINE) {
                throw new Exception($"Service '{_hostSvcAppId}' did not come online in time.");
            }

            await RequestPicture();

            _logger.LogInformation("DebugPayloadApp completed at: {time}", DateTimeOffset.Now);
        }
    }

    private void ListHeardServices() {
        _client.ServicesOnline().ForEach((pulse) => {
            if (_appsOnline.Contains(pulse.AppId)) return;
            _appsOnline.Add(pulse.AppId);
            _logger.LogInformation($"App:...{pulse.AppId}...");
        });
    }

    private async Task RequestPicture() {
        DateTime maxTimeToWait = DateTime.Now.Add(MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        TaskingResponse? response = null;


        var trackingId = Guid.NewGuid().ToString();

        Microsoft.Azure.SpaceFx.PlanetaryComputerGeotiff.EarthImageRequest imageRequest = new() {
            GeographicCoordinates = new Microsoft.Azure.SpaceFx.PlanetaryComputerGeotiff.GeographicCoordinates() {
                Latitude = (float) 47.6062,
                Longitude = (float) -122.3321
            },
            Collection = "landsat-c2-l2"
        };

        imageRequest.Asset.Add("red");
        imageRequest.Asset.Add("blue");
        imageRequest.Asset.Add("green");

        TaskingRequest request = new() {
            RequestHeader = new RequestHeader() {
                TrackingId = trackingId,
                CorrelationId = trackingId
            },
            SensorID = Microsoft.Azure.SpaceFx.VTH.Plugins.PlanetaryComputerVTHPlugin.SENSOR_ID,
            RequestData = Google.Protobuf.WellKnownTypes.Any.Pack(imageRequest)
        };

        // Register a callback event to catch the response
        void responseEventHandler(object? _, TaskingResponse _response) {
            if (_response.ResponseHeader.TrackingId != request.RequestHeader.TrackingId) return;
            _logger.LogInformation($"......Heard {typeof(TaskingResponse).Name} (Tracking ID: '{request.RequestHeader.TrackingId}')...");
            response = _response;
            MessageHandler<Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.TaskingResponse>.MessageReceivedEvent -= responseEventHandler;
        }

        MessageHandler<Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.TaskingResponse>.MessageReceivedEvent += responseEventHandler;

        _logger.LogInformation("Requesting sensors...");

        await _client.DirectToApp(appId: _hostSvcAppId, message: request);

        while (response == null && DateTime.Now <= maxTimeToWait) {
            await Task.Delay(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {typeof(TaskingResponse).Name} after {MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {_hostSvcAppId} is deployed");

        if (response.ResponseHeader.Status != Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful) throw new Exception(string.Format("Picture Request failed.  Failure: {0}", response.ResponseHeader.Message));

        _logger.LogInformation("Tasking response: " + response.ResponseHeader.Status);


    }
}