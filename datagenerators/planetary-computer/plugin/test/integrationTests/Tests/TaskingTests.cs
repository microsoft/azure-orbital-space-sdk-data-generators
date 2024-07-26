namespace Microsoft.Azure.SpaceFx.VTH.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class TaskingTests : IClassFixture<TestSharedContext>
{
    readonly TestSharedContext _context;

    public TaskingTests(TestSharedContext context)
    {
        _context = context;
    }

    [Fact]
    public async Task TaskingQueryAndResponse()
    {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.HostServices.Sensor.TaskingResponse? response = null;

        var trackingId = Guid.NewGuid().ToString();
        var request = new MessageFormats.HostServices.Sensor.TaskingRequest()
        {
            RequestHeader = new()
            {
                TrackingId = trackingId,
            },
            SensorID = "TestSensorAlpha"
        };

        // Register a callback event to catch the response
        void TaskingResponseEventHandler(object? _, MessageFormats.HostServices.Sensor.TaskingResponse _response)
        {
            response = _response;
            MessageHandler<MessageFormats.HostServices.Sensor.TaskingResponse>.MessageReceivedEvent -= TaskingResponseEventHandler;
        }

        MessageHandler<MessageFormats.HostServices.Sensor.TaskingResponse>.MessageReceivedEvent += TaskingResponseEventHandler;


        await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, request);


        while (response == null && DateTime.Now <= maxTimeToWait)
        {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} heartbeat after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotEqual(MessageFormats.Common.StatusCodes.Successful, response.ResponseHeader.Status);
    }

    private async Task RequestPicture()
    {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.HostServices.Sensor.TaskingResponse? response = null;
        MessageFormats.HostServices.Sensor.SensorData? data_response = null;


        var trackingId = Guid.NewGuid().ToString();

        Microsoft.Azure.SpaceFx.PlanetaryComputerGeotiff.EarthImageRequest imageRequest = new()
        {
            GeographicCoordinates = new Microsoft.Azure.SpaceFx.PlanetaryComputerGeotiff.GeographicCoordinates()
            {
                Latitude = (float)47.6062,
                Longitude = (float)-122.3321
            },
            Collection = "landsat-c2-l2"
        };

        imageRequest.Asset.Add("red");
        imageRequest.Asset.Add("blue");
        imageRequest.Asset.Add("green");

        MessageFormats.HostServices.Sensor.TaskingRequest request = new()
        {
            RequestHeader = new MessageFormats.Common.RequestHeader()
            {
                TrackingId = trackingId,
                CorrelationId = trackingId
            },
            SensorID = Microsoft.Azure.SpaceFx.VTH.Plugins.PlanetaryComputerVTHPlugin.SENSOR_ID,
            RequestData = Google.Protobuf.WellKnownTypes.Any.Pack(imageRequest)
        };

        // Register a callback event to catch the response
        void responseEventHandler(object? _, MessageFormats.HostServices.Sensor.TaskingResponse _response)
        {
            if (_response.ResponseHeader.TrackingId != request.RequestHeader.TrackingId) return;
            response = _response;
            MessageHandler<Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.TaskingResponse>.MessageReceivedEvent -= responseEventHandler;
        }

        // Register a callback event to catch the response
        void sensorDataEventHandler(object? _, MessageFormats.HostServices.Sensor.SensorData _response)
        {
            if (_response.ResponseHeader.TrackingId != request.RequestHeader.TrackingId) return;
            data_response = _response;
            MessageHandler<Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.SensorData>.MessageReceivedEvent -= sensorDataEventHandler;
        }

        MessageHandler<Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.TaskingResponse>.MessageReceivedEvent += responseEventHandler;

        MessageHandler<Microsoft.Azure.SpaceFx.MessageFormats.HostServices.Sensor.SensorData>.MessageReceivedEvent += sensorDataEventHandler;

        await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, request);

        while (response == null && DateTime.Now <= maxTimeToWait)
        {
            await Task.Delay(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {typeof(MessageFormats.HostServices.Sensor.TaskingResponse).Name} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        if (response.ResponseHeader.Status != Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful) throw new Exception(string.Format("Picture Request failed.  Failure: {0}", response.ResponseHeader.Message));


        Assert.Equal(MessageFormats.Common.StatusCodes.Successful, response.ResponseHeader.Status);

        while (data_response == null && DateTime.Now <= maxTimeToWait)
        {
            await Task.Delay(100);
        }

        if (data_response == null) throw new TimeoutException($"Failed to hear {typeof(MessageFormats.HostServices.Sensor.SensorData).Name} after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        if (data_response.ResponseHeader.Status != Microsoft.Azure.SpaceFx.MessageFormats.Common.StatusCodes.Successful) throw new Exception(string.Format("Picture Request failed.  Failure: {0}", response.ResponseHeader.Message));


        Assert.Equal(MessageFormats.Common.StatusCodes.Successful, data_response.ResponseHeader.Status);
    }
}