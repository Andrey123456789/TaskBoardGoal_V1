using TaskBoard.Domain.Entities;

namespace TaskBoard.Domain.Tests;

[TestFixture]
public sealed class TaskRelationTests
{
    [Test]
    public void Create_EitherDirection_ProducesSameCanonicalPair()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var forward = TaskRelation.Create(a, b);
        var backward = TaskRelation.Create(b, a);

        Assert.Multiple(() =>
        {
            Assert.That(forward.FirstTaskId, Is.EqualTo(backward.FirstTaskId));
            Assert.That(forward.SecondTaskId, Is.EqualTo(backward.SecondTaskId));
            Assert.That(forward.FirstTaskId.CompareTo(forward.SecondTaskId), Is.LessThan(0));
        });
    }

    [Test]
    public void Create_SameTaskOnBothSides_Throws()
    {
        var taskId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => TaskRelation.Create(taskId, taskId));
    }

    [Test]
    public void GetOtherTaskId_ReturnsCounterpartFromEitherSide()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var relation = TaskRelation.Create(a, b);

        Assert.Multiple(() =>
        {
            Assert.That(relation.GetOtherTaskId(a), Is.EqualTo(b));
            Assert.That(relation.GetOtherTaskId(b), Is.EqualTo(a));
            Assert.That(relation.Involves(a) && relation.Involves(b), Is.True);
            Assert.Throws<ArgumentException>(() => relation.GetOtherTaskId(Guid.NewGuid()));
        });
    }
}
