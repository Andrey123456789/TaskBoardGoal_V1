using TaskBoard.Domain.Entities;

namespace TaskBoard.Domain.Tests;

[TestFixture]
public sealed class UserTests
{
    [Test]
    public void Create_OrdinaryUser_IsNotDeletedUser()
    {
        var user = User.Create("Jane", "jane@example.com", DateTimeOffset.UnixEpoch);

        Assert.Multiple(() =>
        {
            Assert.That(user.IsDeletedUser, Is.False);
            Assert.That(user.Id, Is.Not.EqualTo(User.DeletedUserId));
        });
    }

    [TestCase("Deleted User")]
    [TestCase(" deleted USER ")]
    public void Create_WithReservedName_Throws(string name)
    {
        Assert.Multiple(() =>
        {
            Assert.That(User.IsReservedName(name), Is.True);
            Assert.Throws<ArgumentException>(() => User.Create(name, "x@example.com", DateTimeOffset.UnixEpoch));
        });
    }

    [Test]
    public void Update_ChangesNameAndEmail()
    {
        var user = User.Create("Jane", "jane@example.com", DateTimeOffset.UnixEpoch);

        user.Update("Jane Doe", "jane.doe@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(user.Name, Is.EqualTo("Jane Doe"));
            Assert.That(user.Email, Is.EqualTo("jane.doe@example.com"));
        });
    }
}
