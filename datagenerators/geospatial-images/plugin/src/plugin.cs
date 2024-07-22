using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link;
using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Position;
using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor;

namespace Microsoft.Azure.SpaceFx.PlatformServices.MessageTranslationService.Plugins;
public class GeospatialImagesPlugin : Microsoft.Azure.SpaceFx.VTH.Plugins.PluginBase {

    // HelloWorld sensor is a simple request/reply sensor to validate the direct path scenario works
    const string SENSOR_ID = "GeospatialImages";
    private readonly string OUTPUT_DIR = "";
    private readonly string IMAGES_DIR = "";
    private readonly HttpClient HTTP_CLIENT;
    private readonly ConcurrentQueue<TaskingRequest> IMAGE_QUEUE = new();
    private readonly ConcurrentDictionary<string, LinkRequest> LinkRequestIDs = new();

    public GeospatialImagesPlugin() {
        LoggerFactory loggerFactory = new();
        this.Logger = loggerFactory.CreateLogger<GeospatialImagesPlugin>();
        ServiceProvider? serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider() ?? throw new Exception("Unable to initialize the HTTP Client Factory");
        HTTP_CLIENT = serviceProvider.GetService<IHttpClientFactory>().CreateClient();
        OUTPUT_DIR = Core.GetXFerDirectories().Result.outbox_directory;
    }

    public override void ConfigureLogging(ILoggerFactory loggerFactory) => this.Logger = loggerFactory.CreateLogger<GeospatialImagesPlugin>();

    public override ILogger Logger { get; set; }

    public override Task BackgroundTask() => Task.Run(async () => {
        Logger.LogInformation("{pluginName}: {methodRequest} Background Task started.",
            nameof(GeospatialImagesPlugin), nameof(BackgroundTask));

        DateTime maxTimeToWaitForLinkResponse;

        while (true) {
            if (IMAGE_QUEUE.TryDequeue(out var taskingRequest)) {
                try {
                    Logger.LogDebug("{pluginName}: {methodRequest} Processing {messageType} (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                        nameof(GeospatialImagesPlugin), nameof(BackgroundTask), nameof(TaskingRequest), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

                    SensorData sensorData = await processImageRequest(taskingRequest);

                    if (sensorData.ResponseHeader.Status == StatusCodes.Successful) {

                        Logger.LogDebug("{pluginName}: {methodRequest} Waiting for successful Link Response for all files (maximum 30 seconds) (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                            nameof(GeospatialImagesPlugin), nameof(BackgroundTask), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

                        bool hasPendingLinkRequests = true;
                        maxTimeToWaitForLinkResponse = DateTime.Now.Add(TimeSpan.FromSeconds(30));

                        while (hasPendingLinkRequests && DateTime.Now <= maxTimeToWaitForLinkResponse) {
                            hasPendingLinkRequests = LinkRequestIDs.Any(i => i.Value.RequestHeader.CorrelationId == sensorData.ResponseHeader.CorrelationId);
                            System.Threading.Thread.Sleep(100); ;
                        }

                        if (hasPendingLinkRequests) {
                            sensorData.ResponseHeader.Status = StatusCodes.Timeout;
                            sensorData.ResponseHeader.Message = $"Timeout while transmitting files to '{taskingRequest.RequestHeader.AppId}'.  Check LinkService is deployed and operational, then retry your query.";
                        }
                    }

                    Logger.LogInformation("{pluginName}: {methodRequest} Sending results to {appId} (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                        nameof(GeospatialImagesPlugin), nameof(BackgroundTask), taskingRequest.RequestHeader.AppId, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

                    Core.DirectToApp(appId: taskingRequest.RequestHeader.AppId, message: sensorData);
                } catch (Exception ex) {
                    Logger.LogError("{pluginName}: {methodRequest} Error processing tasking request.  Error message: {errorMsg} (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                        nameof(GeospatialImagesPlugin), nameof(BackgroundTask), ex.ToString(), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);
                }
            } else {
                System.Threading.Thread.Sleep(1000);
            }
        }
    });

    public override Task<LinkResponse?> LinkResponse(LinkResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: received and processed a LinkResponse Event", nameof(GeospatialImagesPlugin));
        return (input_response ?? null);
    });

    public override Task<(PositionUpdateRequest?, PositionUpdateResponse?)> PositionUpdateRequest(PositionUpdateRequest? input_request, PositionUpdateResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a PositionUpdateRequest Event", nameof(GeospatialImagesPlugin));
        return (input_request, input_response);
    });

    public override Task<PositionUpdateResponse?> PositionUpdateResponse(PositionUpdateResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a PositionUpdateResponse Event", nameof(GeospatialImagesPlugin));
        return (input_response ?? null);
    });

    public override Task<PluginHealthCheckResponse> PluginHealthCheckResponse() => Task<PluginHealthCheckResponse>.Run(() => {
        return new MessageFormats.Common.PluginHealthCheckResponse {
            ResponseHeader = new MessageFormats.Common.ResponseHeader {
                CorrelationId = Guid.NewGuid().ToString(),
                TrackingId = Guid.NewGuid().ToString(),
                Status = MessageFormats.Common.StatusCodes.Healthy,
                Message = "geospatial-images-vth-plugin is operational"
            },
        };
    });

    public override Task<SensorData?> SensorData(SensorData? input_request) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a SensorData Event", nameof(GeospatialImagesPlugin ));
        return (input_request ?? null);
    });

    public override Task<(SensorsAvailableRequest?, SensorsAvailableResponse?)> SensorsAvailableRequest(SensorsAvailableRequest? input_request, SensorsAvailableResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a SensorsAvailableResponse Event", nameof(GeospatialImagesPlugin));

        if (input_request == null || input_response == null) return (input_request, input_response);

        input_response.ResponseHeader.Status = StatusCodes.Successful;
        input_response.Sensors.Add(new SensorsAvailableResponse.Types.SensorAvailable() { SensorID = SENSOR_ID });
        input_response.Sensors.Add(new SensorsAvailableResponse.Types.SensorAvailable() { SensorID = SENSOR_TEMPERATURE_ID });


        return (input_request, input_response);
    });

    public override Task<SensorsAvailableResponse?> SensorsAvailableResponse(SensorsAvailableResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a SensorsAvailableResponse Event", nameof(GeospatialImagesPlugin));
        return (input_response ?? null);
    });

    public override Task<(TaskingPreCheckRequest?, TaskingPreCheckResponse?)> TaskingPreCheckRequest(TaskingPreCheckRequest? input_request, TaskingPreCheckResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a TaskingPreCheckRequest Event", nameof(GeospatialImagesPlugin));
        if (input_request == null || input_response == null) return (input_request, input_response);

        // Flip it to success
        input_response.ResponseHeader.Status = StatusCodes.Successful;
        return (input_request, input_response);
    });

    public override Task<TaskingPreCheckResponse?> TaskingPreCheckResponse(TaskingPreCheckResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a TaskingPreCheckResponse Event", nameof(GeospatialImagesPlugin));
        return (input_response ?? null);
    });

    public override Task<(TaskingRequest?, TaskingResponse?)> TaskingRequest(TaskingRequest? input_request, TaskingResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a TaskingRequest Event", nameof(GeospatialImagesPlugin));
        if (input_request == null || input_response == null) return (input_request, input_response);

        // Flip it to success
        input_response.ResponseHeader.Status = StatusCodes.Successful;
        input_response.SensorID = input_request.SensorID;

        // Add the client ID to the list so we can direct send it Sensor Data
        if (!CLIENT_IDS.Contains(input_request.RequestHeader.AppId))
            CLIENT_IDS.Add(input_request.RequestHeader.AppId);

        // Flip it to success
        input_response.ResponseHeader.Status = StatusCodes.Successful;
        return (input_request, input_response);
    });

    public override Task<TaskingResponse?> TaskingResponse(TaskingResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a SensorsAvailableRequest Event", nameof(GeospatialImagesPlugin));
        return (input_response ?? null);
    });
}
