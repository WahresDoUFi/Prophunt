using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        None,
        Waiting,
        Running
    }

    public enum Team
    {
        None,
        Hunter,
        Props
    }

    [Header("References")]
    [SerializeField] private Transform[] spawnLocations;
    [SerializeField] private NetworkObject propPrefab;
    [SerializeField] private NetworkObject hunterPrefab;
    [SerializeField] private CinemachineCamera lobbyCamera;

    private HunterController hunter;

    public float RemainingTime => _remainingTime.Value;
    public float HunterSpawnDelay => _hunterSpawnDelay.Value;
    public float NextTaunt => _nextTaunt.Value;
    public ulong HunterClientId => _hunterClientId.Value;
    public Team WinningTeam => _winningTeam.Value;
    public GameState State => NetworkManager.IsConnectedClient ? _gameState.Value : GameState.None;

    private readonly NetworkVariable<GameState> _gameState = new(GameState.Waiting);
    private readonly NetworkVariable<ulong> _hunterClientId = new(ulong.MaxValue);
    private readonly NetworkVariable<float> _remainingTime = new();
    private readonly NetworkVariable<float> _hunterSpawnDelay = new();
    private float _tauntFrequency;
    private readonly NetworkVariable<float> _nextTaunt = new();
    private readonly NetworkVariable<Team> _winningTeam = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        lobbyCamera.Priority = HunterClientId == NetworkManager.LocalClientId ? 3 : 0;
        if (!IsHost) return;
        if (State != GameState.Running) return;

        if (_hunterSpawnDelay.Value > 0)
        {
            _hunterSpawnDelay.Value -= Time.deltaTime;
            if (_hunterSpawnDelay.Value <= 0f)
            {
                SpawnHunter();
            }
        } else if (_remainingTime.Value > 0)
        {
            _remainingTime.Value -= Time.deltaTime;
            _nextTaunt.Value -= Time.deltaTime;

            if (_nextTaunt.Value <= 0f)
            {
                _nextTaunt.Value += _tauntFrequency;
                foreach (var prop in PropController.AliveProps)
                {
                    prop.Taunt();
                }
            }

            if (_remainingTime.Value <= 0f)
            {
                //  game over
                _winningTeam.Value = Team.Props;
                DespawnPlayerObjects();
                _gameState.Value = GameState.Waiting;
            }
        }
    }

    public void InitializeGame(float gameTime, float hunterSpawnDelay, float tauntFrequency, byte maxRerolls)
    {
        _remainingTime.Value = gameTime;
        _hunterSpawnDelay.Value = hunterSpawnDelay;
        _nextTaunt.Value = tauntFrequency;
        _tauntFrequency = tauntFrequency;
        StartNextRound(maxRerolls);
    }

    public void PropDied()
    {
        if (!IsHost) return;

        if (PropController.AliveProps.Count > 0) return;
        _winningTeam.Value = Team.Hunter;
        _gameState.Value = GameState.Waiting;
        DespawnPlayerObjects();
    }

    private void StartNextRound(byte rerolls)
    {
        if (!HasAuthority) return;

        if (hunter != null)
        {
            hunter.NetworkObject.Despawn();
        }
        foreach (var prop in PropController.AliveProps)
        {
            prop.NetworkObject.Despawn();
        }

        var nextHunterIds = new List<ulong>(NetworkManager.ConnectedClientsIds);
        nextHunterIds.Remove(_hunterClientId.Value);
        _hunterClientId.Value = nextHunterIds[Random.Range(0, nextHunterIds.Count)];

        var spawnLocations = new List<Transform>(this.spawnLocations);

        foreach (var clientId in NetworkManager.ConnectedClientsIds)
        {
            if (clientId == _hunterClientId.Value) continue;

            var location = spawnLocations[Random.Range(0, spawnLocations.Count)];
            spawnLocations.Remove(location);

            var prop = NetworkManager.SpawnManager.InstantiateAndSpawn(propPrefab, clientId, position: location.position, rotation: location.rotation);
            prop.GetComponent<PropController>().MaxRerolls = rerolls;
        }

        _gameState.Value = GameState.Running;
    }

    private void SpawnHunter()
    {
        var location = spawnLocations[Random.Range(0, spawnLocations.Count())];
        NetworkManager.SpawnManager.InstantiateAndSpawn(hunterPrefab, _hunterClientId.Value, position: location.position, rotation: location.rotation);
    }

    private void DespawnPlayerObjects()
    {
        foreach (PropController prop in PropController.AliveProps)
        {
            prop.NetworkObject.Despawn();
        }

        HunterController.Instance.NetworkObject.Despawn();
    }
}
