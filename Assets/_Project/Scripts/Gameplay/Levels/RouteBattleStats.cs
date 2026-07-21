namespace CatGuard.Gameplay.Levels
{
    public sealed class RouteBattleStats
    {
        public RouteBattleStats(string routeId)
        {
            RouteId = routeId ?? string.Empty;
        }

        public string RouteId { get; }
        public int Spawned { get; private set; }
        public int Defeated { get; private set; }
        public int Escaped { get; private set; }
        public float FirstSpawnTime { get; private set; } = -1f;
        public float LastSpawnTime { get; private set; } = -1f;

        public void RecordSpawn(float timestamp)
        {
            if (Spawned == 0)
            {
                FirstSpawnTime = timestamp;
            }

            LastSpawnTime = timestamp;
            Spawned++;
        }

        public void RecordDefeat()
        {
            Defeated++;
        }

        public void RecordEscape()
        {
            Escaped++;
        }
    }
}
