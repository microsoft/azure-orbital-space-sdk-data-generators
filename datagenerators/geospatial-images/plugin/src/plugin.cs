using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Link;
using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Position;
using Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor;

namespace Microsoft.Azure.SpaceFx.VTH.Plugins;
public class GeospatialImagesPlugin : Microsoft.Azure.SpaceFx.VTH.Plugins.PluginBase {

    // HelloWorld sensor is a simple request/reply sensor to validate the direct path scenario works
    public const string SENSOR_ID = "GeospatialImages";
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
        if (input_response == null) return input_response;

        LinkRequestIDs.TryRemove(input_response.ResponseHeader.CorrelationId, out _);

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
        Logger.LogInformation("{pluginName}: Plugin received and processed a SensorData Event", nameof(GeospatialImagesPlugin));
        return (input_request ?? null);
    });

    public override Task<(SensorsAvailableRequest?, SensorsAvailableResponse?)> SensorsAvailableRequest(SensorsAvailableRequest? input_request, SensorsAvailableResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a SensorsAvailableResponse Event", nameof(GeospatialImagesPlugin));

        if (input_request == null || input_response == null) return (input_request, input_response);

        input_response.ResponseHeader.Status = StatusCodes.Successful;
        input_response.Sensors.Add(new SensorsAvailableResponse.Types.SensorAvailable() { SensorID = SENSOR_ID });


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
        Logger.LogInformation("{pluginName}: {methodRequest} received tasking request.  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(TaskingRequest), input_request.RequestHeader.TrackingId, input_request.RequestHeader.CorrelationId);
        if (input_request == null) return (input_request, input_response);
        if (!input_request.SensorID.Equals(SENSOR_ID, StringComparison.InvariantCultureIgnoreCase)) return (input_request, input_response); // This is not the plugin you're looking for



        Logger.LogTrace("{pluginName}: {methodRequest} Adding request to queue.  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(TaskingRequest), input_request.RequestHeader.TrackingId, input_request.RequestHeader.CorrelationId);

        IMAGE_QUEUE.Enqueue(input_request);


        // Flip it to success
        input_response.ResponseHeader.Status = StatusCodes.Successful;
        input_response.SensorID = input_request.SensorID;


        Logger.LogDebug("{pluginName}: {methodRequest} Setting {image_response_type} status to {status}.  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(TaskingRequest), input_response.GetType().Name, input_response.ResponseHeader.Status, input_request.RequestHeader.TrackingId, input_request.RequestHeader.CorrelationId);

        Logger.LogDebug("{pluginName}: {methodRequest} Returning {image_response_type} to VTH.  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(TaskingRequest), input_response.GetType().Name, input_request.RequestHeader.TrackingId, input_request.RequestHeader.CorrelationId);

        return (input_request, input_response);
    });

    public override Task<TaskingResponse?> TaskingResponse(TaskingResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("{pluginName}: Plugin received and processed a SensorsAvailableRequest Event", nameof(GeospatialImagesPlugin));
        return (input_response ?? null);
    });

    // Helper Functions:
    private async Task<SensorData> processImageRequest(TaskingRequest taskingRequest) {
        GeospatialImages.EarthImageRequest imageRequest;
        GeospatialImages.EarthImageResponse imageResponse;
        string fileName;

        SensorData sensorData = new() {
            ResponseHeader = new ResponseHeader() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = taskingRequest.RequestHeader.CorrelationId
            },
            DestinationAppId = taskingRequest.RequestHeader.AppId,
            TaskingTrackingId = taskingRequest.RequestHeader.TrackingId,
            SensorID = SENSOR_ID
        };

        // Unpack the request
        try {
            Logger.LogTrace("{pluginName}: {methodRequest} Extracting {embeddedMessageType} from {messageType}.  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(processImageRequest), nameof(GeospatialImages.EarthImageRequest), nameof(TaskingRequest), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

            imageRequest = taskingRequest.RequestData.Unpack<GeospatialImages.EarthImageRequest>();

            Logger.LogDebug("{pluginName}: {methodRequest} Successfully extracted {embeddedMessageType} from {messageType}.  Request Object: {requestObject}  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(processImageRequest), nameof(GeospatialImages.EarthImageRequest), nameof(TaskingRequest), Google.Protobuf.JsonFormatter.Default.Format(imageRequest), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

        } catch (Exception ex) {
            Logger.LogError("{pluginName}: {methodRequest} Failed to extract {embeddedMessageType} from {messageType}.  Error: {errMsg}  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(processImageRequest), nameof(GeospatialImages.EarthImageRequest), nameof(TaskingRequest), ex.Message, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

            sensorData.ResponseHeader.Status = StatusCodes.GeneralFailure;
            sensorData.ResponseHeader.Message = string.Format($"{nameof(GeospatialImagesPlugin)}: {nameof(TaskingRequest)} Failed to extract {nameof(GeospatialImages.EarthImageRequest)} from {nameof(TaskingRequest)}.  Error: {ex.Message}");
            return sensorData;
        }

        // Query the Geotiff Processor Tool
        try {
            Logger.LogTrace("{pluginName}: {methodRequest} Querying tool-image-provider computer.  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(processImageRequest), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

            string imageType = imageRequest.ImageType.ToString().ToLower();

            Logger.LogDebug("{pluginName}: {methodRequest} requesting {imageType}.  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                nameof(GeospatialImagesPlugin), nameof(processImageRequest), imageType, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);


            // var datagenerator_url = string.Format("http://datagenerator-geospatial-images.svc.cluster.local:8080/get_geotiff?lat={0}&lon={1}", imageRequest.LineOfSight.Latitude.ToString(), imageRequest.LineOfSight.Longitude.ToString());
            var datagenerator_url = string.Format("http://datagenerator-geospatial-images.platformsvc.svc.cluster.local:8080/get_{0}?lat={1}&lon={2}", imageType, imageRequest.LineOfSight.Latitude.ToString(), imageRequest.LineOfSight.Longitude.ToString());


            Logger.LogDebug("{pluginName}: {methodRequest} Query: {datagenerator_url} .  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(processImageRequest), datagenerator_url, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

            fileName = await downloadFile(url: datagenerator_url, filePath: Path.Combine(OUTPUT_DIR, taskingRequest.RequestHeader.TrackingId));


        } catch (Exception ex) {
            Logger.LogError("{pluginName}: {methodRequest} Failed to query datagenerator-geospatial-images.  Error: {errMsg}  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(GeospatialImagesPlugin), nameof(processImageRequest), ex.Message, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

            sensorData.ResponseHeader.Status = MessageFormats.Common.StatusCodes.GeneralFailure;
            sensorData.ResponseHeader.Message = string.Format($"{nameof(GeospatialImagesPlugin)}: {nameof(TaskingRequest)} Failed to extract {nameof(GeospatialImages.EarthImageRequest)} from {nameof(TaskingRequest)}.  Error: {ex.Message}");
            return sensorData;
        }

        // Build the sensor data message to return to the calling method
        imageResponse = new() {
            LineOfSight = imageRequest.LineOfSight,
            Filename = fileName
        };

        DateTime maxTimeToWaitForFile = DateTime.Now.Add(TimeSpan.FromSeconds(30));

        Logger.LogDebug("{pluginName}: {methodRequest} waiting for {filePath}.  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
            nameof(GeospatialImagesPlugin), nameof(processImageRequest), Path.Combine(OUTPUT_DIR, imageResponse.Filename), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

        while (!File.Exists(Path.Combine(OUTPUT_DIR, imageResponse.Filename)) && DateTime.Now <= maxTimeToWaitForFile) {
            await Task.Delay(100);
        }


        if (File.Exists(Path.Combine(OUTPUT_DIR, imageResponse.Filename))) {
            Logger.LogInformation("{pluginName}: {methodRequest} Found file at '{filePath}'.  Sending link request to '{destinationAppId}'. (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                   nameof(GeospatialImagesPlugin), nameof(processImageRequest), Path.Combine(OUTPUT_DIR, imageResponse.Filename), taskingRequest.RequestHeader.AppId, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

            LinkRequest linkRequest = new() {
                DestinationAppId = taskingRequest.RequestHeader.AppId,
                ExpirationTime = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(1)),
                FileName = fileName,
                LeaveSourceFile = false,
                LinkType = LinkRequest.Types.LinkType.App2App,
                Priority = Priority.Medium,
                RequestHeader = new() {
                    TrackingId = Guid.NewGuid().ToString(),
                    CorrelationId = sensorData.ResponseHeader.CorrelationId
                }
            };

            if (taskingRequest.RequestHeader.Metadata.FirstOrDefault((_item) => _item.Key == "SOURCE_PAYLOAD_APP_ID").Value != null) {
                string sourcePayloadAppID = taskingRequest.RequestHeader.Metadata.FirstOrDefault((_item) => _item.Key == "SOURCE_PAYLOAD_APP_ID").Value;
                linkRequest.RequestHeader.Metadata.Add("SOURCE_PAYLOAD_APP_ID", sourcePayloadAppID);
            }

            LinkRequestIDs.TryAdd(sensorData.ResponseHeader.CorrelationId, linkRequest);
            await Core.DirectToApp(appId: $"hostsvc-{nameof(HostServices.Link)}", message: linkRequest);

        } else {
            sensorData.ResponseHeader.Status = StatusCodes.Timeout;
            sensorData.ResponseHeader.Message = $"Timeout while waiting for file to land in {OUTPUT_DIR}.  Check the tool-geotiff-processor pod logs for errors and troubleshoot";
        }

        Logger.LogInformation("{pluginName}: {methodRequest} Returning generated sensor data. (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                   nameof(GeospatialImagesPlugin), nameof(processImageRequest), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

        sensorData.ResponseHeader.Status = StatusCodes.Successful;
        sensorData.Data = Any.Pack(imageResponse);

        return sensorData;
    }

    private async Task<string> downloadFile(string url, string filePath) {
        using (HttpResponseMessage response = await HTTP_CLIENT.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)) {
            response.EnsureSuccessStatusCode();
            string contentType = response.Content.Headers.ContentType.MediaType.Split('/')[1];
            string fullFilePath = filePath + "." + contentType;


            using (Stream streamToReadFrom = await response.Content.ReadAsStreamAsync()) {
                using (Stream streamToWriteTo = File.Open(fullFilePath, FileMode.Create)) {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    streamToWriteTo.Close();
                }
                streamToReadFrom.Close();
            }
            response.Dispose();

            return Path.GetFileName(fullFilePath);
        }
    }
}
