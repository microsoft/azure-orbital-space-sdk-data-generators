namespace Microsoft.Azure.SpaceFx.VTH.IntegrationTests.Tests;

[Collection(nameof(TestSharedContext))]
public class SensorsAvailableTests : IClassFixture<TestSharedContext> {
    readonly TestSharedContext _context;

    public SensorsAvailableTests(TestSharedContext context) {
        _context = context;
    }

    [Fact]
    public async Task SensorsAvailableRequest() {
        DateTime maxTimeToWait = DateTime.Now.Add(TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG);
        MessageFormats.HostServices.Sensor.SensorsAvailableResponse? response = null;

        // Register a callback event to catch the response
        void SensorsAvailableResponseEventHandler(object? _, MessageFormats.HostServices.Sensor.SensorsAvailableResponse _response) {
            response = _response;
            MessageHandler<MessageFormats.HostServices.Sensor.SensorsAvailableResponse>.MessageReceivedEvent -= SensorsAvailableResponseEventHandler;
        }

        MessageHandler<MessageFormats.HostServices.Sensor.SensorsAvailableResponse>.MessageReceivedEvent += SensorsAvailableResponseEventHandler;


        MessageFormats.HostServices.Sensor.SensorsAvailableRequest testMessage = new() {
            RequestHeader = new MessageFormats.Common.RequestHeader() {
                TrackingId = Guid.NewGuid().ToString(),
                CorrelationId = Guid.NewGuid().ToString()
            },
        };


        await TestSharedContext.SPACEFX_CLIENT.DirectToApp(TestSharedContext.TARGET_SVC_APP_ID, testMessage);


        Console.WriteLine($"Sending '{testMessage.GetType().Name}' (TrackingId: '{testMessage.RequestHeader.TrackingId}')");

        while (response == null && DateTime.Now <= maxTimeToWait) {
            Thread.Sleep(100);
        }

        if (response == null) throw new TimeoutException($"Failed to hear {nameof(response)} heartbeat after {TestSharedContext.MAX_TIMESPAN_TO_WAIT_FOR_MSG}.  Please check that {TestSharedContext.TARGET_SVC_APP_ID} is deployed");

        Assert.NotNull(response);
    }
}