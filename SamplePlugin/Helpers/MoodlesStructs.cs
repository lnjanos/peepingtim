global using MoodlesStatusInfo = (
    int Version,
    System.Guid GUID,
    int IconID,
    string Title,
    string Description,
    string CustomVFXPath,
    long ExpireTicks,
    StatusType Type,
    int Stacks,
    int StackSteps,
    uint Modifiers,
    System.Guid ChainedStatus,
    ChainTrigger ChainTrigger,
    string Applier,
    string Dispeller,
    bool Permanent
);

public enum StatusType
{
    Positive, Negative, Special
}

public enum ChainTrigger
{
    Dispel = 0,
    HitMaxStacks = 1,
    TimerExpired = 2,
}
