namespace SPT_FairBotHealth;

public sealed record FairBotHealthConfig
{
    public static FairBotHealthConfig Default => new() { GlobalMultiplier = 1 };
    
    public double? GlobalMultiplier { get; init; }
    
    public BodyParts? BodyPartMultipliers { get; init; }

    public sealed record BodyParts
    {
        public double? Chest { get; init; }
        public double? Head { get; init; }
        public double? Stomach { get; init; }
        public double? LeftLeg { get; init; }
        public double? RightLeg { get; init; }
        public double? LeftArm { get; init; }
        public double? RightArm { get; init; }
    }
}