using ArmoryManager.Core;
using Xunit;

namespace ArmoryManager.Tests;

public sealed class ArmoryScanStateTests
{
    [Fact]
    public void RepeatedRequestsCoalesceIntoOnePendingRequest()
    {
        var state = new ArmoryScanState();

        state.Request();
        state.Request();
        state.Request();
        state.ConsumeRequest();

        Assert.False(state.HasPendingRequest);
    }

    [Fact]
    public void DragAndSuspensionBothBlockMutation()
    {
        var state = new ArmoryScanState();
        state.StartDragging();
        Assert.False(state.CanMutate(isRunner: true, currentTime: 10f));

        state.EndDragging();
        state.SuspendUntil(20f);
        Assert.False(state.CanMutate(isRunner: true, currentTime: 19.99f));
        Assert.True(state.CanMutate(isRunner: true, currentTime: 20f));
        Assert.False(state.CanMutate(isRunner: false, currentTime: 30f));
    }

    [Fact]
    public void AscensionCancellationClearsOnlyPendingWork()
    {
        var state = new ArmoryScanState();
        state.Request();
        state.SuspendUntil(5f);
        state.CancelPendingRequest();

        Assert.False(state.HasPendingRequest);
        Assert.False(state.CanMutate(isRunner: true, currentTime: 4f));
    }
}
