using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace SPT_FairBotHealth;

public sealed record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.waldfee.spt.fairbothealth";
    public override string Name { get; init; } = "Fair Bot Health";
    public override string Author { get; init; } = "waldfee";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 999)]
public sealed class BossFairnessMod(ISptLogger<BossFairnessMod> logger, DatabaseService databaseService) : IOnLoad
{
    private const string LogPrefix = "[FairBotHealth] ";
    
    public Task OnLoad()
    {
        try
        {
            SetBotHealth();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.Error($"{LogPrefix}Error while adjusting bot health.", ex);
            throw;
        }
    }

    private void SetBotHealth()
    {
        var playerHealth = databaseService.GetGlobals().Configuration.Health.ProfileHealthSettings.BodyPartsSettings;

        foreach (var (botTypeName, botType) in databaseService.GetBots().Types)
        {
            if (botType == null)
                continue;
            
            logger.Debug($"{LogPrefix}Adjusting bot type {botTypeName}");
            
            foreach (var bodyPart in botType.BotHealth.BodyParts)
            {
                SetAdjustedValue(bodyPart.Chest, playerHealth.Chest.Maximum);
                SetAdjustedValue(bodyPart.Head, playerHealth.Head.Maximum);
                SetAdjustedValue(bodyPart.Stomach, playerHealth.Stomach.Maximum);
                SetAdjustedValue(bodyPart.LeftLeg, playerHealth.LeftLeg.Maximum);
                SetAdjustedValue(bodyPart.RightLeg, playerHealth.RightLeg.Maximum);
                SetAdjustedValue(bodyPart.LeftArm, playerHealth.LeftArm.Maximum);
                SetAdjustedValue(bodyPart.RightArm, playerHealth.RightArm.Maximum);
            }
        }
        
        logger.Success($"{LogPrefix}Finished adjusting bot health.");

        void SetAdjustedValue(MinMax<double> botHealth, double clampAt)
        {
            var newValue = Math.Min(botHealth.Min, clampAt);
            botHealth.Min = newValue;
            botHealth.Max = newValue;
        }
    }
}