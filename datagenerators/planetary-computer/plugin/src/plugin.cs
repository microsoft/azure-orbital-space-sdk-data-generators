namespace Microsoft.Azure.SpaceFx.VTH.Plugins;
public class PlanetaryComputerVTHPlugin : Microsoft.Azure.SpaceFx.VTH.Plugins.PluginBase {

    private readonly string OUTPUT_DIR = "";
    public const string SENSOR_ID = "PlanetaryComputer";
    private readonly string DATA_GENERATOR_URL = "http://datagenerator-planetary-computer.platformsvc.svc.cluster.local:8080";
    private readonly HttpClient HTTP_CLIENT;
    private readonly ConcurrentQueue<TaskingRequest> IMAGE_QUEUE = new();
    private readonly ConcurrentDictionary<string, LinkRequest> LinkRequestIDs = new();
    public PlanetaryComputerVTHPlugin() {
        LoggerFactory loggerFactory = new();
        this.Logger = loggerFactory.CreateLogger<PlanetaryComputerVTHPlugin>();
        ServiceProvider? serviceProvider = new Extensions.DependencyInjection.ServiceCollection().AddHttpClient().BuildServiceProvider() ?? throw new Exception("Unable to initialize the HTTP Client Factory");
        HTTP_CLIENT = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient();
        OUTPUT_DIR = Core.GetXFerDirectories().Result.outbox_directory;
    }

    public override void ConfigureLogging(ILoggerFactory loggerFactory) => this.Logger = loggerFactory.CreateLogger<PlanetaryComputerVTHPlugin>();

    public override ILogger Logger { get; set; }

    public override Task BackgroundTask() => Task.Run(async () => {
        // Log the start of the background task
        Logger.LogDebug("{pluginName}: {methodRequest} Background Task started.",
            nameof(PlanetaryComputerVTHPlugin), nameof(BackgroundTask));

        // Loop indefinitely to process image requests from the queue
        while (true) {
            if (IMAGE_QUEUE.TryDequeue(out var taskingRequest)) {
                try {
                    // Log the processing start of a tasking request
                    Logger.LogDebug("{pluginName}: {methodRequest} Processing {messageType} (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                        nameof(PlanetaryComputerVTHPlugin), nameof(BackgroundTask), nameof(TaskingRequest), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

                    // Process the image request synchronously and log completion
                    SensorData sensorData = await processImageRequest(taskingRequest);

                    Logger.LogDebug("{pluginName}: {methodRequest} Completed processing of {messageType} (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                        nameof(PlanetaryComputerVTHPlugin), nameof(BackgroundTask), nameof(TaskingRequest), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

                    // If processing was successful, wait for link responses with a timeout
                    if (sensorData.ResponseHeader.Status == StatusCodes.Successful) {
                        Logger.LogDebug("{pluginName}: {methodRequest} Waiting for successful Link Response for all files (maximum 30 seconds) (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                            nameof(PlanetaryComputerVTHPlugin), nameof(BackgroundTask), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

                        DateTime deadline = DateTime.Now.AddSeconds(30);
                        while (DateTime.Now <= deadline && LinkRequestIDs.Any(i => i.Value.RequestHeader.CorrelationId == sensorData.ResponseHeader.CorrelationId)) {
                            await Task.Delay(250); // Brief pause before we check again
                        }

                        // If the deadline is exceeded, update the sensor data status to timeout
                        if (DateTime.Now > deadline) {
                            sensorData.ResponseHeader.Status = StatusCodes.Timeout;
                            sensorData.ResponseHeader.Message = "Timeout while transmitting files to MTS. Check LinkService is deployed and operational, then retry your query.";
                        }
                    }

                    // Send the results back to the requesting application
                    Logger.LogInformation("{pluginName}: {methodRequest} Sending results to {appId} (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                        nameof(PlanetaryComputerVTHPlugin), nameof(BackgroundTask), taskingRequest.RequestHeader.AppId, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

                    await Core.DirectToApp(appId: taskingRequest.RequestHeader.AppId, message: sensorData);
                } catch (Exception ex) {
                    // Log any errors that occur during processing
                    Logger.LogError("{pluginName}: {methodRequest} Error processing tasking request. Error message: {errorMsg} (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                        nameof(PlanetaryComputerVTHPlugin), nameof(BackgroundTask), ex.ToString(), taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);
                }
            } else {
                // If the queue is empty, wait for a second before trying again
                await Task.Delay(1000);
            }
        }
    });

    public override Task<LinkResponse?> LinkResponse(LinkResponse? input_response) => Task.Run(() => {
        if (input_response == null) return input_response;

        LinkRequestIDs.TryRemove(input_response.ResponseHeader.TrackingId, out _);

        return input_response;
    });

    public override Task<(PositionUpdateRequest?, PositionUpdateResponse?)> PositionUpdateRequest(PositionUpdateRequest? input_request, PositionUpdateResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("Plugin received and processed a PositionUpdateRequest Event");
        return (input_request, input_response);
    });

    public override Task<PositionUpdateResponse?> PositionUpdateResponse(PositionUpdateResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("Plugin received and processed a PositionUpdateResponse Event");
        return (input_response ?? null);
    });

    public override Task<PluginHealthCheckResponse> PluginHealthCheckResponse() => Task<PluginHealthCheckResponse>.Run(() => {
        return new MessageFormats.Common.PluginHealthCheckResponse {
            ResponseHeader = new MessageFormats.Common.ResponseHeader {
                CorrelationId = Guid.NewGuid().ToString(),
                TrackingId = Guid.NewGuid().ToString(),
                Status = MessageFormats.Common.StatusCodes.Healthy,
                Message = "Hello from the plugin!"
            },
        };
    });

    // Return as-is since we aren't doing any processing on this event
    public override Task<SensorsAvailableResponse?> SensorsAvailableResponse(SensorsAvailableResponse? input_response) => Task.FromResult(input_response);
    public override Task<TaskingResponse?> TaskingResponse(TaskingResponse? input_response) => Task.FromResult(input_response);
    public override Task<TaskingPreCheckResponse?> TaskingPreCheckResponse(TaskingPreCheckResponse? input_response) => Task.FromResult(input_response);
    public override Task<SensorData?> SensorData(SensorData? input_request) => Task.FromResult(input_request);


    public override Task<(SensorsAvailableRequest?, SensorsAvailableResponse?)> SensorsAvailableRequest(SensorsAvailableRequest? input_request, SensorsAvailableResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("Plugin received and processed a SensorsAvailableResponse Event");

        if (input_request == null || input_response == null) return (input_request, input_response);

        input_response.ResponseHeader.Status = StatusCodes.Successful;
        input_response.Sensors.Add(new SensorsAvailableResponse.Types.SensorAvailable() { SensorID = SENSOR_ID });

        return (input_request, input_response);
    });



    public override Task<(TaskingPreCheckRequest?, TaskingPreCheckResponse?)> TaskingPreCheckRequest(TaskingPreCheckRequest? input_request, TaskingPreCheckResponse? input_response) => Task.Run(() => {
        Logger.LogInformation("Plugin received and processed a TaskingPreCheckRequest Event");
        if (input_request == null || input_response == null) return (input_request, input_response);

        // Flip it to success
        input_response.ResponseHeader.Status = StatusCodes.Successful;
        return (input_request, input_response);
    });



    public override Task<(TaskingRequest?, TaskingResponse?)> TaskingRequest(TaskingRequest? input_request, TaskingResponse? input_response) => Task.Run(() => {
        // Log the receipt of a TaskingRequest Event
        Logger.LogInformation("Plugin received and processed a TaskingRequest Event");

        // Validate input_request and input_response are not null and input_request is for the correct sensor
        if (input_request == null || input_response == null || !input_request.SensorID.Equals(SENSOR_ID, StringComparison.InvariantCultureIgnoreCase)) {
            return (input_request, input_response);
        }

        // Log the receipt of the tasking request with tracking and correlation IDs
        Logger.LogInformation("{pluginName}: {methodRequest} received tasking request. (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(PlanetaryComputerVTHPlugin), nameof(TaskingRequest), input_request.RequestHeader.TrackingId, input_request.RequestHeader.CorrelationId);

        // Add the request to the processing queue
        IMAGE_QUEUE.Enqueue(input_request);

        // Update the response status to successful and set the SensorID
        input_response.ResponseHeader.Status = StatusCodes.Successful;
        input_response.SensorID = input_request.SensorID;

        // Log the successful processing of the request
        Logger.LogDebug("{pluginName}: {methodRequest} Setting {image_response_type} status to {status} and returning to VTH. (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(PlanetaryComputerVTHPlugin), nameof(TaskingRequest), nameof(TaskingResponse), StatusCodes.Successful, input_request.RequestHeader.TrackingId, input_request.RequestHeader.CorrelationId);

        // Return the modified request and response
        return (input_request, input_response);
    });



    // Define an asynchronous method to process image requests
    private async Task<SensorData> processImageRequest(TaskingRequest taskingRequest) {
        // Initialize sensor data with default values
        var sensorData = new SensorData {
            ResponseHeader = new ResponseHeader {
                TrackingId = Guid.NewGuid().ToString(), // Generate a new tracking ID
                CorrelationId = taskingRequest.RequestHeader.CorrelationId, // Use the correlation ID from the request
                Status = StatusCodes.Successful // Assume success initially
            },
            DestinationAppId = taskingRequest.RequestHeader.AppId, // Set the destination app ID from the request
            TaskingTrackingId = taskingRequest.RequestHeader.TrackingId, // Set the tasking tracking ID from the request
            SensorID = SENSOR_ID // Set the sensor ID from a constant
        };

        // Declare variables for the planetary image request and response
        PlanetaryComputerGeotiff.EarthImageRequest planetaryImageRequest;
        PlanetaryComputerGeotiff.EarthImageResponse planetaryImageResponse = new();

        try {
            // Unpack the request data into a planetary image request object
            planetaryImageRequest = taskingRequest.RequestData.Unpack<PlanetaryComputerGeotiff.EarthImageRequest>();
            planetaryImageResponse.OriginalRequest = planetaryImageRequest; // Store the original request in the response

            // Query the planetary computer for images based on the request
            var planetaryComputerResults = await queryPlanetaryComputerForImages(planetaryImageRequest, taskingRequest.RequestHeader);

            // Iterate over the results and process each image
            foreach (var (asset, url) in planetaryComputerResults) {
                // Generate a file name for the image
                var calculatedFileName = $"{taskingRequest.RequestHeader.TrackingId}_{asset}.tiff";
                // Download the file to the output directory
                await downloadFile(url, Path.Combine(OUTPUT_DIR, calculatedFileName));
                // Add the image file information to the response
                planetaryImageResponse.ImageFiles.Add(new PlanetaryComputerGeotiff.EarthImageResponse.Types.ImageFile {
                    Asset = asset,
                    FileName = calculatedFileName
                });
                // Create and send a link request for the downloaded file
                await createAndSendLinkRequest(taskingRequest.RequestHeader.AppId, calculatedFileName, sensorData.ResponseHeader.CorrelationId);
            }
        } catch (Exception ex) {
            // If an exception occurs, update the response status and message
            Logger.LogError("{pluginName}: {methodRequest} Error processing request.  Error: {error}. (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                    nameof(PlanetaryComputerVTHPlugin), nameof(processImageRequest), ex.Message, taskingRequest.RequestHeader.TrackingId, taskingRequest.RequestHeader.CorrelationId);

            sensorData.ResponseHeader.Status = StatusCodes.GeneralFailure;
            sensorData.ResponseHeader.Message = $"{nameof(PlanetaryComputerVTHPlugin)}: Error processing request: {ex.Message}";
            return sensorData; // Return the sensor data with the error information
        }

        // Pack the planetary image response into the sensor data
        sensorData.Data = Any.Pack(planetaryImageResponse);
        return sensorData; // Return the sensor data with the successful response
    }

    // Define an asynchronous method to create and send a link request
    private async Task createAndSendLinkRequest(string appId, string fileName, string correlationId) {
        // Generate a new tracking ID
        var trackingId = Guid.NewGuid().ToString();

        // Initialize a new link request with specified parameters
        var linkRequest = new LinkRequest {
            DestinationAppId = appId, // Set the destination application ID
            ExpirationTime = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(1)), // Set the expiration time to 1 hour from now
            FileName = fileName, // Set the file name to be linked
            LeaveSourceFile = false, // Indicate that the source file should not be left after linking
            LinkType = LinkRequest.Types.LinkType.App2App, // Set the link type to application-to-application
            Priority = Priority.Medium, // Set the priority of the link request to medium
            RequestHeader = new RequestHeader {
                TrackingId = trackingId, // Use the generated tracking ID
                CorrelationId = correlationId // Use the provided correlation ID
            }
        };

        // Send the link request to the appropriate service
        await Core.DirectToApp($"hostsvc-{nameof(Microsoft.Azure.SpaceFx.MessageFormats.Common.HostServices.Link)}", linkRequest);

        // Attempt to add the link request to a concurrent dictionary for tracking
        LinkRequestIDs.TryAdd(trackingId, linkRequest);
    }



    // Define an asynchronous method to query the Planetary Computer for images based on geographic coordinates
    private async Task<Dictionary<string, string>> queryPlanetaryComputerForImages(PlanetaryComputerGeotiff.EarthImageRequest imageRequest, RequestHeader requestHeader) {
        // Construct the URL for querying the Planetary Computer with the specified image collection and geographic coordinates
        var url = $"{DATA_GENERATOR_URL}/{imageRequest.Collection}/items/{imageRequest.GeographicCoordinates.Latitude}/{imageRequest.GeographicCoordinates.Longitude}?{generatePlanetaryComputerQueryString(imageRequest)}";

        // Log the URL being queried along with tracking and correlation IDs
        Logger.LogDebug("{pluginName}: {methodRequest} Querying planetary computer: {url}  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
            nameof(PlanetaryComputerVTHPlugin), nameof(queryPlanetaryComputerForImages), url, requestHeader.TrackingId, requestHeader.CorrelationId);

        // Perform the HTTP GET request to the Planetary Computer
        using var response = await HTTP_CLIENT.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        // Ensure the response status code indicates success
        response.EnsureSuccessStatusCode();
        // Deserialize the JSON response into a list of dictionaries, each representing an image asset
        var results = await response.Content.ReadFromJsonAsync<List<Dictionary<string, string>>>() ?? throw new Exception("Failed to deserialize Planetary Computer Results");

        // Extract the first result from the list as the primary result
        var firstResult = results.First();
        // Log each key-value pair (asset and its URL) from the first result
        foreach (var kvp in firstResult) {
            Logger.LogDebug("{pluginName}: {methodRequest} Adding result '{asset}': '{url}'  (TrackingId: {trackingId}, CorrelationId: {correlationId})",
                nameof(PlanetaryComputerVTHPlugin), nameof(queryPlanetaryComputerForImages), kvp.Key, kvp.Value, requestHeader.TrackingId, requestHeader.CorrelationId);
        }

        // Return the first result as a dictionary of image assets and their URLs
        return firstResult;
    }

    // Generates a query string for querying the Planetary Computer based on the provided EarthImageRequest
    private string generatePlanetaryComputerQueryString(PlanetaryComputerGeotiff.EarthImageRequest imageRequest) {
        var uriBuilder = new UriBuilder(); // Initialize a new UriBuilder to construct the query string
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty); // Initialize an empty query string collection

        // Iterate over each asset in the request and add it to the query string
        foreach (var asset in imageRequest.Asset) {
            query.Add("asset", asset);
        }

        // If MaxItems is specified (not 0), add it to the query string
        if (imageRequest.MaxItems != 0) query["max_items"] = imageRequest.MaxItems.ToString();

        // If ordering is specified, add both "order" and "order_by" to the query string
        if (imageRequest.Order != PlanetaryComputerGeotiff.EarthImageRequest.Types.Order.None && !string.IsNullOrWhiteSpace(imageRequest.OrderBy)) {
            query["order"] = imageRequest.Order.ToString();
            query["order_by"] = imageRequest.OrderBy;
        }

        // If both MinTime and MaxTime are specified, add "time_range" to the query string
        if (imageRequest.MinTime != null && imageRequest.MaxTime != null) {
            query["time_range"] = $"{imageRequest.MinTime.ToDateTime():yyyy-MM-dd}/{imageRequest.MaxTime.ToDateTime():yyyy-MM-dd}";
        }

        // If "top" is specified (greater than 0), add it to the query string
        if (imageRequest.Top > 0) query["top"] = imageRequest.Top.ToString();

        // Assign the constructed query string to the UriBuilder
        uriBuilder.Query = query.ToString();

        // Return the query component of the URI, trimming the leading '?' character
        return uriBuilder.Uri.Query.TrimStart('?');
    }

    // Downloads a file from a given URL to a specified file path asynchronously
    private async Task downloadFile(string url, string filePath) {
        // Initiate an HTTP GET request to the specified URL
        using var response = await HTTP_CLIENT.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        // Ensure the HTTP response indicates success
        response.EnsureSuccessStatusCode();
        // Read the response content as a stream
        using var streamToReadFrom = await response.Content.ReadAsStreamAsync();
        // Open a file stream to write the content to the specified file path
        using var streamToWriteTo = File.Open(filePath, FileMode.Create);
        // Copy the content from the HTTP response stream to the file stream
        await streamToReadFrom.CopyToAsync(streamToWriteTo);
    }
}
