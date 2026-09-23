using System.Net;
using System.Net.Http.Json;
using TaskBoard.Application.DTOs.Users;
using TaskBoard.Domain.Entities;
using TaskBoard.IntegrationTests.Infrastructure;

namespace TaskBoard.IntegrationTests.Users;

[TestFixture]
public sealed class UsersApiTests : ApiTestBase
{
    [Test]
    public async Task CreateUser_WithValidRequest_Returns201WithLocationAndUser()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new { name = "  Jane Doe  ", email = " jane.doe@example.com " },
            Json);

        var created = await ReadAsync<UserDto>(response, HttpStatusCode.Created);

        Assert.Multiple(() =>
        {
            Assert.That(created.Name, Is.EqualTo("Jane Doe"));
            Assert.That(created.Email, Is.EqualTo("jane.doe@example.com"));
            Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"/api/users/{created.Id}"));
        });

        var fetched = await ReadAsync<UserDto>(await Client.GetAsync($"/api/users/{created.Id}"), HttpStatusCode.OK);
        Assert.That(fetched, Is.EqualTo(created));
    }

    [Test]
    public async Task CreateUser_WithDuplicateEmailInDifferentCase_Returns409()
    {
        await CreateUserAsync(email: "jane@example.com");

        var response = await Client.PostAsJsonAsync(
            "/api/users",
            new { name = "Other Jane", email = "JANE@Example.COM" },
            Json);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "user.duplicate_email");
    }

    [Test]
    public async Task CreateUser_WithInvalidEmail_Returns400WithFieldError()
    {
        var response = await Client.PostAsJsonAsync("/api/users", new { name = "Jane", email = "not-an-email" }, Json);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That(problem.GetProperty("errors").TryGetProperty("email", out _), Is.True);
    }

    [Test]
    public async Task CreateUser_WithoutName_Returns400WithFieldError()
    {
        var response = await Client.PostAsJsonAsync("/api/users", new { email = "jane@example.com" }, Json);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.That(problem.GetProperty("errors").TryGetProperty("name", out _), Is.True);
    }

    [Test]
    public async Task UpdateUser_WithValidRequest_Returns204AndPersistsChanges()
    {
        var user = await CreateUserAsync("Jane", "jane@example.com");

        var response = await Client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new { name = "Jane Smith", email = "jane.smith@example.com" },
            Json);

        await AssertStatusAsync(response, HttpStatusCode.NoContent);

        var fetched = await ReadAsync<UserDto>(await Client.GetAsync($"/api/users/{user.Id}"), HttpStatusCode.OK);
        Assert.Multiple(() =>
        {
            Assert.That(fetched.Name, Is.EqualTo("Jane Smith"));
            Assert.That(fetched.Email, Is.EqualTo("jane.smith@example.com"));
            Assert.That(fetched.CreatedAt, Is.EqualTo(user.CreatedAt));
        });
    }

    [Test]
    public async Task UpdateUser_KeepingOwnEmailWithDifferentCase_Returns204()
    {
        var user = await CreateUserAsync("Jane", "jane@example.com");

        var response = await Client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new { name = "Jane", email = "Jane@Example.com" },
            Json);

        await AssertStatusAsync(response, HttpStatusCode.NoContent);
    }

    [Test]
    public async Task UpdateUser_ToEmailOfAnotherUser_Returns409()
    {
        await CreateUserAsync("Jane", "jane@example.com");
        var john = await CreateUserAsync("John", "john@example.com");

        var response = await Client.PutAsJsonAsync(
            $"/api/users/{john.Id}",
            new { name = "John", email = "jane@example.com" },
            Json);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "user.duplicate_email");
    }

    [Test]
    public async Task UpdateUser_UnknownId_Returns404()
    {
        var response = await Client.PutAsJsonAsync(
            $"/api/users/{Guid.NewGuid()}",
            new { name = "Jane", email = "jane@example.com" },
            Json);

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "user.not_found");
    }

    [Test]
    public async Task DeleteUser_WithoutTasks_Returns204AndRemovesUser()
    {
        var user = await CreateUserAsync();

        await AssertStatusAsync(await Client.DeleteAsync($"/api/users/{user.Id}"), HttpStatusCode.NoContent);

        await AssertProblemAsync(await Client.GetAsync($"/api/users/{user.Id}"), HttpStatusCode.NotFound);
        Assert.That(await ListUsersAsync(), Is.Empty);
    }

    [Test]
    public async Task DeleteUser_UnknownId_Returns404()
    {
        await AssertProblemAsync(
            await Client.DeleteAsync($"/api/users/{Guid.NewGuid()}"),
            HttpStatusCode.NotFound,
            "user.not_found");
    }

    [Test]
    public async Task ListUsers_ReturnsOrdinaryUsersOnly()
    {
        var bob = await CreateUserAsync("Bob", "bob@example.com");
        var alice = await CreateUserAsync("Alice", "alice@example.com");

        var users = await ListUsersAsync();

        Assert.Multiple(() =>
        {
            Assert.That(users.Select(x => x.Id), Is.EqualTo(new[] { alice.Id, bob.Id }));
            Assert.That(users.Select(x => x.Id), Does.Not.Contain(User.DeletedUserId));
            Assert.That(users.Select(x => x.Name), Does.Not.Contain(User.DeletedUserName));
        });
    }

    [Test]
    public async Task ListUsers_WithNoOrdinaryUsers_ReturnsEmptyArray()
    {
        var response = await Client.GetAsync("/api/users");

        await AssertStatusAsync(response, HttpStatusCode.OK);
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("[]"));
    }

    [TestCase("Deleted User")]
    [TestCase("  deleted user ")]
    public async Task CreateUser_WithReservedDeletedUserName_Returns422(string name)
    {
        var response = await Client.PostAsJsonAsync("/api/users", new { name, email = "someone@example.com" }, Json);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "user.reserved_name");
        Assert.That(await ListUsersAsync(), Is.Empty);
    }

    [Test]
    public async Task UpdateUser_ToReservedDeletedUserName_Returns422()
    {
        var user = await CreateUserAsync("Jane", "jane@example.com");

        var response = await Client.PutAsJsonAsync(
            $"/api/users/{user.Id}",
            new { name = "DELETED USER", email = "jane@example.com" },
            Json);

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "user.reserved_name");
    }

    [Test]
    public async Task DeletedUser_CannotBeReadEditedOrDeletedThroughUserManagement()
    {
        await AssertProblemAsync(
            await Client.GetAsync($"/api/users/{User.DeletedUserId}"),
            HttpStatusCode.NotFound);

        await AssertProblemAsync(
            await Client.PutAsJsonAsync(
                $"/api/users/{User.DeletedUserId}",
                new { name = "Renamed", email = "renamed@example.com" },
                Json),
            HttpStatusCode.NotFound);

        await AssertProblemAsync(
            await Client.DeleteAsync($"/api/users/{User.DeletedUserId}"),
            HttpStatusCode.NotFound);

        var deletedUser = await Factory.QueryDatabaseAsync(db =>
            Task.FromResult(db.Users.Single(x => x.Id == User.DeletedUserId)));

        Assert.That(deletedUser.Name, Is.EqualTo(User.DeletedUserName));
    }
}
