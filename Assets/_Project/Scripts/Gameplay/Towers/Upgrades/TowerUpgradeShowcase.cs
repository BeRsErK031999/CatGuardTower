using CatGuard.Gameplay.Towers;
using UnityEngine;

namespace CatGuard.Gameplay.Towers.Upgrades
{
    [ExecuteAlways]
    public sealed class TowerUpgradeShowcase : MonoBehaviour
    {
        [SerializeField] private TowerConfig[] towerRoster = System.Array.Empty<TowerConfig>();

        public void ConfigureRoster(TowerConfig[] towers)
        {
            towerRoster = towers ?? System.Array.Empty<TowerConfig>();
            Rebuild();
        }

        public void Rebuild()
        {
            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                if (Application.isPlaying)
                {
                    Destroy(transform.GetChild(index).gameObject);
                }
                else
                {
                    DestroyImmediate(transform.GetChild(index).gameObject);
                }
            }

            for (var row = 0; row < towerRoster.Length; row++)
            {
                var towerConfig = towerRoster[row];
                if (towerConfig?.BattleUpgradeTree == null || towerConfig.BattleUpgradeTree.Branches.Count < 2)
                {
                    continue;
                }

                CreateTower(towerConfig, null, 0, new Vector3(-3.4f, 2.8f - row * 1.4f, 0f), "Base");
                var branchA = towerConfig.BattleUpgradeTree.Branches[0];
                var branchB = towerConfig.BattleUpgradeTree.Branches[1];
                CreateTower(towerConfig, branchA, branchA.MaximumTier, new Vector3(0f, 2.8f - row * 1.4f, 0f), "BranchA");
                CreateTower(towerConfig, branchB, branchB.MaximumTier, new Vector3(3.4f, 2.8f - row * 1.4f, 0f), "BranchB");
            }
        }

        private void CreateTower(
            TowerConfig towerConfig,
            TowerUpgradeBranchConfig branch,
            int tier,
            Vector3 position,
            string suffix)
        {
            var towerObject = new GameObject($"{towerConfig.TowerId}_{suffix}");
            towerObject.transform.SetParent(transform, false);
            towerObject.transform.localPosition = position;
            var tower = towerObject.AddComponent<BasicTower>();
            tower.Initialize(null, towerConfig);
            if (branch != null)
            {
                tower.ApplyUpgradeForPreview(branch.BranchId, tier);
            }
        }
    }
}
