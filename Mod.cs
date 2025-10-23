using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

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
public sealed class BossFairnessMod(
    ISptLogger<BossFairnessMod> logger,
    DatabaseService databaseService,
    ModHelper modHelper,
    JsonUtil jsonUtil) : IOnLoad
{
    private const string LogPrefix = "[FairBotHealth] ";
    
    public async Task OnLoad()
    {
        try
        {
            await SetBotHealth();
        }
        catch (Exception ex)
        {
            logger.Error($"{LogPrefix}Error while adjusting bot health.", ex);
            throw;
        }
    }

    private async Task SetBotHealth()
    {
        var playerHealth = databaseService.GetGlobals().Configuration.Health.ProfileHealthSettings.BodyPartsSettings;
        var config = await GetConfig();
        
        foreach (var (botTypeName, botType) in databaseService.GetBots().Types)
        {
            if (botType == null)
                continue;
            
            logger.Debug($"{LogPrefix}Adjusting bot type {botTypeName}");
            
            foreach (var bodyPart in botType.BotHealth.BodyParts)
            {
                SetAdjustedValue(bodyPart.Chest, playerHealth.Chest.Maximum * config.BodyPartMultipliers?.Chest ?? config.GlobalMultiplier ?? 1);
                SetAdjustedValue(bodyPart.Head, playerHealth.Head.Maximum * config.BodyPartMultipliers?.Head ?? config.GlobalMultiplier ?? 1);
                SetAdjustedValue(bodyPart.Stomach, playerHealth.Stomach.Maximum * config.BodyPartMultipliers?.Stomach ?? config.GlobalMultiplier ?? 1);
                SetAdjustedValue(bodyPart.LeftLeg, playerHealth.LeftLeg.Maximum * config.BodyPartMultipliers?.LeftLeg ?? config.GlobalMultiplier ?? 1);
                SetAdjustedValue(bodyPart.RightLeg, playerHealth.RightLeg.Maximum * config.BodyPartMultipliers?.RightLeg ?? config.GlobalMultiplier ?? 1);
                SetAdjustedValue(bodyPart.LeftArm, playerHealth.LeftArm.Maximum * config.BodyPartMultipliers?.LeftArm ?? config.GlobalMultiplier ?? 1);
                SetAdjustedValue(bodyPart.RightArm, playerHealth.RightArm.Maximum * config.BodyPartMultipliers?.RightArm ?? config.GlobalMultiplier ?? 1);
            }
        }
        
        if (config.GlobalMultiplier != null)
            logger.Success($"{LogPrefix}Finished adjusting bot health with {config.GlobalMultiplier:F1} multiplier.");
        else if (config.BodyPartMultipliers != null)
            logger.Success(
                $"{LogPrefix}Finished adjusting bot health with {config.BodyPartMultipliers.Head:F1}, {config.BodyPartMultipliers.Chest:F1}, {config.BodyPartMultipliers.Stomach:F1}, {config.BodyPartMultipliers.LeftArm:F1}, {config.BodyPartMultipliers.RightArm:F1}, {config.BodyPartMultipliers.LeftLeg:F1}, {config.BodyPartMultipliers.RightLeg:F1} multipliers.");

        void SetAdjustedValue(MinMax<double> botHealth, double clampAt)
        {
            var newValue = Math.Min(botHealth.Min, clampAt);
            botHealth.Min = newValue;
            botHealth.Max = newValue;
        }
    }

    private async Task<FairBotHealthConfig> GetConfig()
    {
        var modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var configPath = Path.Combine(modPath, "Config", "config.jsonc");
        var config = await jsonUtil.DeserializeFromFileAsync<FairBotHealthConfig>(configPath);
        
        if (config != null)
        {
            if (config.GlobalMultiplier != null &&
                (config.BodyPartMultipliers?.Chest != null
                 || config.BodyPartMultipliers?.RightArm != null
                 || config.BodyPartMultipliers?.LeftArm != null
                 || config.BodyPartMultipliers?.LeftLeg != null
                 || config.BodyPartMultipliers?.RightLeg != null
                 || config.BodyPartMultipliers?.Head != null
                 || config.BodyPartMultipliers?.Stomach != null))
            {
                logger.Warning(
                    $"{LogPrefix}Both {nameof(FairBotHealthConfig.GlobalMultiplier)} and {nameof(FairBotHealthConfig.BodyPartMultipliers)} configured, choose one. Using fallback default.");
                return FairBotHealthConfig.Default;
            }

            if (config.GlobalMultiplier == null &&
                (config.BodyPartMultipliers?.Chest == null
                 || config.BodyPartMultipliers?.RightArm == null
                 || config.BodyPartMultipliers?.LeftArm == null
                 || config.BodyPartMultipliers?.LeftLeg == null
                 || config.BodyPartMultipliers?.RightLeg == null
                 || config.BodyPartMultipliers?.Head == null
                 || config.BodyPartMultipliers?.Stomach == null))
            {
                logger.Warning(
                    $"{LogPrefix}Both {nameof(FairBotHealthConfig.GlobalMultiplier)} and {nameof(FairBotHealthConfig.BodyPartMultipliers)} are missing in the config, choose one. Using fallback default.");
                return FairBotHealthConfig.Default;
            }
            
            return config;
        }

        logger.Warning($"{LogPrefix}No config file found, fallback to defaults.");
        return FairBotHealthConfig.Default;
    }
}