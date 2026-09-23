using TaskBoard.Application.DTOs.Users;
using TaskBoard.Application.Validation;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Tests.Validation;

[TestFixture]
public sealed class SaveUserRequestValidatorTests
{
    private readonly SaveUserRequestValidator _validator = new();

    [TestCase("jane@example.com")]
    [TestCase("Jane.Doe+work@sub.example.co.uk")]
    [TestCase("  jane@example.com  ")]
    public void Validate_WithValidEmail_Passes(string email)
    {
        var result = _validator.Validate(new SaveUserRequest("Jane", email));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("not-an-email")]
    [TestCase("jane@")]
    [TestCase("@example.com")]
    [TestCase("jane@localhost")]
    [TestCase("jane@example.")]
    [TestCase("jane@@example.com")]
    [TestCase("Jane <jane@example.com>")]
    [TestCase("jane doe@example.com")]
    public void Validate_WithInvalidEmail_FailsOnEmail(string email)
    {
        var result = _validator.Validate(new SaveUserRequest("Jane", email));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Select(e => e.PropertyName), Is.EquivalentTo(new[] { "Email" }));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Validate_WithMissingNameAndEmail_ReportsBothFields(string? value)
    {
        var result = _validator.Validate(new SaveUserRequest(value!, value!));

        Assert.That(
            result.Errors.Select(e => e.PropertyName).Distinct(),
            Is.EquivalentTo(new[] { "Name", "Email" }));
    }

    [Test]
    public void Validate_WithTooLongName_Fails()
    {
        var result = _validator.Validate(
            new SaveUserRequest(new string('a', User.NameMaxLength + 1), "jane@example.com"));

        Assert.That(result.IsValid, Is.False);
    }
}
