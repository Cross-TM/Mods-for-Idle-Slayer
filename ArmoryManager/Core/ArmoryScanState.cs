namespace ArmoryManager.Core;

internal sealed class ArmoryScanState
{
    internal bool HasPendingRequest { get; private set; }
    internal bool IsDragging { get; private set; }
    internal float SuspendedUntil { get; private set; }

    internal void Request() => HasPendingRequest = true;

    internal void ConsumeRequest() => HasPendingRequest = false;

    internal void CancelPendingRequest() => HasPendingRequest = false;

    internal void StartDragging() => IsDragging = true;

    internal void EndDragging() => IsDragging = false;

    internal void SuspendUntil(float time) => SuspendedUntil = Math.Max(SuspendedUntil, time);

    internal bool CanMutate(bool isRunner, float currentTime) =>
        !IsDragging && currentTime >= SuspendedUntil && isRunner;
}
