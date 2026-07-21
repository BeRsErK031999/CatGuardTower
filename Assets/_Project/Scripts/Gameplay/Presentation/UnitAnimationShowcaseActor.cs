using CatGuard.Gameplay.Enemies;
using UnityEngine;

namespace CatGuard.Gameplay.Presentation
{
    [DisallowMultipleComponent]
    public sealed class UnitAnimationShowcaseActor : MonoBehaviour
    {
        [SerializeField] private EnemyConfig enemyConfig;
        [SerializeField] private UnitAnimationPresenter presenter;

        public EnemyConfig EnemyConfig => enemyConfig;
        public UnitAnimationPresenter Presenter => presenter;

        public void Configure(EnemyConfig enemy, UnitAnimationPresenter animationPresenter)
        {
            enemyConfig = enemy;
            presenter = animationPresenter;
        }
    }
}
