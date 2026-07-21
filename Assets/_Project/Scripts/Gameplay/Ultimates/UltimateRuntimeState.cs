using UnityEngine;

namespace CatGuard.Gameplay.Ultimates
{
    public sealed class UltimateRuntimeState
    {
        public UltimateRuntimeState(UltimateConfig config)
        {
            Config = config;
        }

        public UltimateConfig Config { get; }
        public float Charge { get; private set; }
        public float CooldownRemaining { get; private set; }
        public bool ReadyReported { get; set; }
        public int UseCount { get; private set; }
        public float ChargePercent => Config == null ? 0f : Mathf.Clamp01(Charge / Config.ChargeRequired);
        public bool IsReady => Config != null && Charge >= Config.ChargeRequired && CooldownRemaining <= 0f;

        public void AddCharge(float amount)
        {
            if (Config == null || amount <= 0f)
            {
                return;
            }

            Charge = Mathf.Min(Config.ChargeRequired, Charge + amount);
        }

        public void GrantReady()
        {
            if (Config != null)
            {
                Charge = Config.ChargeRequired;
                CooldownRemaining = 0f;
            }
        }

        public void Tick(float deltaTime)
        {
            CooldownRemaining = Mathf.Max(0f, CooldownRemaining - Mathf.Max(0f, deltaTime));
        }

        public void Consume()
        {
            Charge = 0f;
            CooldownRemaining = Config == null ? 0f : Config.CooldownSeconds;
            ReadyReported = false;
            UseCount++;
        }
    }
}
