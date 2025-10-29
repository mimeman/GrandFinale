using UnityEngine;

// IPoolable 인터페이스를 구현하도록 수정
public abstract class BulletBehaviour : MonoBehaviour, IPoolable
{
    // 자주 사용할 컴포넌트들을 캐싱해둡니다.
    private TrailRenderer _trailRenderer;
    private ParticleSystem[] _particleSystems;
    private Rigidbody _rigidbody;

    [Header("총알 능력치")]
    [Tooltip("총알이 가하는 데미지")]
    public float damage = 10f;
    public string hittableTag = "HitBox"; // 태그 이름 확인 ("Entity" 사용 중)

    protected virtual void Awake()
    {
        // 비활성화된 상태에서도 GetComponentInChildren를 호출하기 위해 true를 사용합니다.
        _trailRenderer = GetComponentInChildren<TrailRenderer>(true);
        _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        _rigidbody = GetComponent<Rigidbody>();
    }

    public virtual void BulletStart(Transform bulletCreator) { }

    // IPoolable 인터페이스 구현
    public void ResetState()
    {
        // 1. Rigidbody 초기화
        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        // 2. Trail Renderer 초기화
        if (_trailRenderer != null)
        {
            // Trail을 즉시 지웁니다.
            _trailRenderer.Clear();
        }

        // 3. Particle Systems 초기화 및 재시작
        if (_particleSystems != null)
        {
            foreach (var ps in _particleSystems)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }
        }

    }
}
