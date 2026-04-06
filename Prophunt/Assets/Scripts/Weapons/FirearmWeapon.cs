using Unity.Netcode;
using UnityEngine;
using Weapons;

public class FirearmWeapon : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject regularImpactPrefab;
    [SerializeField] private GameObject playerImpactPrefab;
    [SerializeField] private AudioSource fireAudio;
    [SerializeField] private AudioSource reloadAudio;

    [Header("Stats")]
    [SerializeField] private int fireRate;
    [SerializeField] private float damage;
    [SerializeField] private float range;
    [SerializeField] private int clipSize;
    [SerializeField] private float reloadTime;
    [SerializeField] private LayerMask hitLayerMask;

    private bool IsBusy => _actionTime > 0;
    private bool HasAmmo => _clip > 0;

    private int _clip;
    private bool _reloading;
    private float _actionTime;

    private void Start()
    {
        _clip = clipSize;
    }

    private void Update()
    {
        _actionTime -= Time.deltaTime;
        if (IsBusy) return;

        if (_reloading)
            _clip = clipSize;

        _reloading = false;
    }

    public void Reload()
    {
        if (_clip < clipSize && !IsBusy)
        {
            _reloading = true;
            _actionTime = reloadTime;
            ReloadSoundRpc();
        }
    }

    public void Fire(Vector3 origin, Vector3 direction)
    {
        if (!HasAmmo) return;
        if (IsBusy) return;

        ShootSoundRpc();
        _actionTime = 1f / (fireRate / 60f);
        _clip--;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitLayerMask))
        {
            ProcessHit(origin, hit);
        }
    }

    private void ProcessHit(Vector3 from, RaycastHit hit)
    {
        Destroy(Instantiate(regularImpactPrefab, hit.point, Quaternion.identity), 1f);
        var direction = (hit.point - from).normalized;
        if (hit.collider.TryGetComponent(out Rigidbody rigidbody))
        {
            var index = SceneProps.GetPropIndex(rigidbody);
            if (index != -1)
            {
                ProcessScenePropHitRpc(index, hit.point, direction * damage);
            } else //   must have been a player prop
            {
                hit.collider.GetComponent<IDamageable>().Damage(damage, hit.point, direction);
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void ShootSoundRpc()
    {
        fireAudio.Play();
    }


    [Rpc(SendTo.Everyone)]
    private void ReloadSoundRpc()
    {
        reloadAudio.Play();
    }

    [Rpc(SendTo.Everyone)]
    private void ProcessScenePropHitRpc(int propIndex, Vector3 hitPoint, Vector3 force)
    {
        SceneProps.GetProp(propIndex).AddForceAtPosition(force, hitPoint, ForceMode.Impulse);
    }
}
