using UnityEngine;
using System.Collections.Generic;

public class BoatLeakSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _particlePrefab;
    [SerializeField] private float _spawnInterval = 5f;
    [SerializeField] private float _destroyRadius = 3f;

    [SerializeField] private Vector2 _spawnAreaMin = new Vector2(-7.62f, -4f);
    [SerializeField] private Vector2 _spawnAreaMax = new Vector2(7.62f, -4.3f);
    [SerializeField] private int _maxParticles = 6;
    [SerializeField] private float _requiredHoldTime = 1.5f;
    [SerializeField] private GameObject _loadingCirclePrefab;
    [SerializeField] private Transform _canvas;
    [SerializeField] private float _leakIncrement = 0.01f;
    [SerializeField] private float _healIncrement = 0.01f;
    [SerializeField] private float _increaseRate = 0.01f;
    [SerializeField] private float _minRate = 1.5f;
    [SerializeField] private float _maxHealth = 10f;
    [SerializeField] private float _pressedTimerStart = 0.4f;
    [SerializeField] private KeyCode _fix1;
    [SerializeField] private KeyCode _fix2;
    public float health = 10f;
	public HealthBar healthBar;

    public Transform[] playerTransforms; 

    private float _spawnTimer;
    private GameObject[] _activeParticleSystems;
    private Dictionary<GameObject, float> _particleHoldTimers = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, GameObject> _particleLoaders = new Dictionary<GameObject, GameObject>();

    private bool _wasHoldingSpace = false;
    private float _pressedTimer = 0f;

    private void Start(){
        _activeParticleSystems = new GameObject[_maxParticles];
        _spawnTimer = _spawnInterval;
        healthBar.SetMaxHealth(_maxHealth);

        #if UNITY_EDITOR
            if(playerTransforms == null){
                Debug.LogWarning("Assign the player transform");
            }
        #endif
    }

    private void Update(){
        // Periodically update the spawn timer and spawn new leaks at the spawn interval
        _spawnInterval -= Time.deltaTime * _increaseRate;
        Mathf.Max(_spawnInterval, _minRate);

        _pressedTimer -= Time.deltaTime;
        _spawnTimer -= Time.deltaTime;
        if(_spawnTimer <= 0){
            SpawnParticleSystem();
            _spawnTimer = _spawnInterval;
        }

        if (_activeParticleSystems != null) {
            for(int i = 0; i < _activeParticleSystems.Length; i++){
                if(_activeParticleSystems[i] != null) {
                    health -= (_leakIncrement) * Time.deltaTime;
                }
            }
        }
        if (!HasActiveLeaks()) {
            health += (_healIncrement) * Time.deltaTime;
            Mathf.Min(health, _maxHealth);
        }
        healthBar.SetHealth(health);

        if(Input.GetKeyDown(_fix1) || Input.GetKeyDown(_fix2)) {
            _pressedTimer = _pressedTimerStart;
        }

        if(Input.GetKey(_fix1) || Input.GetKey(_fix2)) {
            UpdateParticleHoldTimers();
            _wasHoldingSpace = true;
        }
        else if(_wasHoldingSpace) {
            _wasHoldingSpace = false;
            ResetAllParticleTimers();
        }
    }

    private bool HasActiveLeaks() {
        for (int i = 0; i < _activeParticleSystems.Length; i++) {
            if (_activeParticleSystems[i] != null) {
                return true;
            }
        }
        return false;
    }

    
    private void UpdateParticleHoldTimers() {
        for(int i = 0; i < _activeParticleSystems.Length; i++) {
            GameObject particleObj = _activeParticleSystems[i];
            if(particleObj == null) continue;
            
            bool anyPlayerInRange = false;
            Transform closestPlayer = null;
            float closestDistance = float.MaxValue;
            
            for(int j = 0; j < playerTransforms.Length; j++) {
                Vector2 playerPos = new Vector2(playerTransforms[j].position.x, playerTransforms[j].position.y);
                Vector2 particlePos = new Vector2(particleObj.transform.position.x, particleObj.transform.position.y);
                float distance = Vector2.Distance(playerPos, particlePos);
                
                if(distance <= _destroyRadius && distance < closestDistance) {
                    anyPlayerInRange = true;
                    closestPlayer = playerTransforms[j];
                    closestDistance = distance;
                }
            }
            
            if(anyPlayerInRange && closestPlayer != null && (_pressedTimer > 0 || _particleHoldTimers.ContainsKey(particleObj))) {
                if(!_particleHoldTimers.ContainsKey(particleObj)) {
                    _particleHoldTimers[particleObj] = 0f;
                    
                    if(!_particleLoaders.ContainsKey(particleObj) && _loadingCirclePrefab != null) {
                        GameObject loader = Instantiate(_loadingCirclePrefab, 
                                                       particleObj.transform.position + new Vector3(0, 0.5f, 0), 
                                                       Quaternion.identity);
                        loader.transform.SetParent(_canvas);
                        _particleLoaders[particleObj] = loader;
                        loader.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    }
                }
                
                _particleHoldTimers[particleObj] += Time.deltaTime;
                
                if(_particleLoaders.ContainsKey(particleObj)) {
                    UpdateLoadingCircle(particleObj, _particleHoldTimers[particleObj] / _requiredHoldTime);
                }
                
                if(_particleHoldTimers[particleObj] >= _requiredHoldTime) {
                    if(_particleLoaders.ContainsKey(particleObj)) {
                        Destroy(_particleLoaders[particleObj]);
                        _particleLoaders.Remove(particleObj);
                    }
                    
                    Destroy(particleObj);
                    _activeParticleSystems[i] = null;
                    _particleHoldTimers.Remove(particleObj);
                }
            }
            else {
                if(_particleHoldTimers.ContainsKey(particleObj)) {
                    _particleHoldTimers[particleObj] = 0f;
                    
                    if(_particleLoaders.ContainsKey(particleObj)) {
                        UpdateLoadingCircle(particleObj, 0);
                    }
                }
            }
        }
    }
    
    private void UpdateLoadingCircle(GameObject particleObj, float fillAmount) {
        if(_particleLoaders.TryGetValue(particleObj, out GameObject loader)) {
            loader.transform.position = particleObj.transform.position + new Vector3(0, 0.5f, 0);
            
            LoadingCircle loadingCircle = loader.GetComponent<LoadingCircle>();
            if(loadingCircle != null) {
                loadingCircle.SetFillAmount(fillAmount);
            }
        }
    }
    
    private void ResetAllParticleTimers() {
        foreach(var particle in _particleHoldTimers.Keys) {
            if(particle != null && _particleLoaders.ContainsKey(particle)) {
                UpdateLoadingCircle(particle, 0);
            }
        }
        _particleHoldTimers.Clear();
    }
    
    private void SpawnParticleSystem() {
        int emptyIndex = -1;
        for(int i = 0; i < _activeParticleSystems.Length; i++) {
            if(_activeParticleSystems[i] == null) {
                emptyIndex = i;
                break;
            }
        }
        
        if(emptyIndex == -1) {
            return;
        }
        
        Vector3 randomPos = new Vector3(
            Random.Range(_spawnAreaMin.x, _spawnAreaMax.x),
            Random.Range(_spawnAreaMin.y, _spawnAreaMax.y),
            10
        );
        
        GameObject newParticle = Instantiate(_particlePrefab, randomPos, Quaternion.identity);
        ParticleSystem ps = newParticle.GetComponent<ParticleSystem>();
        if(ps != null) ps.Play();
        _activeParticleSystems[emptyIndex] = newParticle;
    }
    
    private void OnDestroy() {
        foreach(var loader in _particleLoaders.Values) {
            if(loader != null) {
                Destroy(loader);
            }
        }
    }
}