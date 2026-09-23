using System.Net;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.ErrorHandling;

[TestFixture]
public sealed class CorsTests : ApiTestBase
{
    [Test]
    public async Task Preflight_FromConfiguredFrontendOrigin_IsAllowed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks/00000000-0000-0000-0000-000000000000/status");
        request.Headers.Add("Origin", TaskBoardApiFactory.AllowedCorsOrigin);
        request.Headers.Add("Access-Control-Request-Method", "PATCH");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await Client.SendAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(
                response.Headers.GetValues("Access-Control-Allow-Origin"),
                Is.EqualTo(new[] { TaskBoardApiFactory.AllowedCorsOrigin }));
            Assert.That(
                string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods")),
                Does.Contain("PATCH"));
        });
    }

    [Test]
    public async Task Request_FromOtherOrigin_GetsNoCorsHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/projects");
        request.Headers.Add("Origin", "http://evil.example.com");

        var response = await Client.SendAsync(request);

        Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
    }
}
