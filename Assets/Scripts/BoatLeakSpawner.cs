using UnityEngine;

public class BoatLeakSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _particlePrefab;
    [SerializeField] private float _spawnInterval = 5f;
    [SerializeField] private float _destroyRadius = 3f;

    [SerializeField] private Vector2 _spawnAreaMin = new Vector2(-7.62f, -3.5f);
    [SerializeField] private Vector2 _spawnAreaMax = new Vector2(7.62f, -3f);
    [SerializeField] private int _maxParticles = 6;
    public Transform[] playerTransforms; 

    private float _spawnTimer;
    private GameObject[] _activeParticleSystems;

    private void Start(){
        _activeParticleSystems = new GameObject[_maxParticles];
        _spawnTimer = _spawnInterval;

        #if UNITY_EDITOR
            if(playerTransforms == null){
                Debug.LogWarning("Assign the player transform");
            }
        #endif
    }

    private void Update(){
        // Periodically update the spawn timer and spawn new leaks at the spawn interval
        _spawnTimer -= Time.deltaTime;
        if(_spawnTimer <= 0){
            SpawnParticleSystem();
            _spawnTimer = _spawnInterval;
        }

        if(Input.GetKeyDown(KeyCode.Space)) CheckForParticleDestruction();
    }
    
    private void SpawnParticleSystem(){
        int emptyIndex = -1;
        for(int i = 0; i < _activeParticleSystems.Length; i++){
            if(_activeParticleSystems[i] == null){
                emptyIndex = i;
                break;
            }
        }
        
        if(emptyIndex == -1){
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
    
    private void CheckForParticleDestruction(){
        for(int i = 0; i < _activeParticleSystems.Length; i++){
            for(int j = 0; j < playerTransforms.Length; j++){
                GameObject particleObj = _activeParticleSystems[i];
                
                if(particleObj == null){
                    continue;
                }
                
                Vector2 playerPos = new Vector2(playerTransforms[j].position.x, playerTransforms[j].position.y);
                Vector2 particlePos = new Vector2(particleObj.transform.position.x, particleObj.transform.position.y);
                float distance = Vector2.Distance(playerPos, particlePos);
                
                if(distance <= _destroyRadius){
                    Destroy(particleObj);
                    _activeParticleSystems[i] = null;
                    break;
                }
            }
        }
    }
}