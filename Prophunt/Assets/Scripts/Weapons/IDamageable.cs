using UnityEngine;

namespace Weapons
{
    public interface IDamageable
    {
        void Damage(float damage, Vector3 point, Vector3 direction);
    }
}
