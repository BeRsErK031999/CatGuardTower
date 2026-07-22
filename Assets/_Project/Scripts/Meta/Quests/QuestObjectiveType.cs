namespace CatGuard.Meta.Quests
{
    public enum QuestCategory
    {
        Tutorial,
        Contract,
        Mastery,
        MapChallenge
    }

    public enum QuestObjectiveType
    {
        WinBattles,
        DefeatEnemies,
        PlaceTowers,
        ReachTowerTier,
        ControlEnemies,
        WinWithoutLifeLoss,
        WinWithMaxTowers,
        WinWithoutSelling,
        UseUltimates,
        WinOnLevel
    }

    public enum QuestStatus
    {
        Active,
        Completed,
        Claimed
    }
}
