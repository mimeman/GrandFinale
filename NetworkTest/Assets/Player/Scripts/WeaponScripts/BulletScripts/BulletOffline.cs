using UnityEngine;

public class BulletOffline : BulletBehaviour
{
    public float lifeTime = 5f; // 총알 생존 시간
    public float startSpeed = 50f; // 초기 속도 (Weapon에서 설정됨)
    public float force = 10f; // 충돌 시 가하는 힘 (Weapon에서 설정됨)
    public GameObject decalPrefab; // 벽 등에 생성될 데칼 프리팹
    public GameObject bloodPrefab; // 몬스터 피격 시 생성될 혈흔 프리팹

    [Tooltip("Linecast가 충돌할 레이어 (예: Environment, Monster 등)")]
    public LayerMask mask;

    private Rigidbody rb;
    private Vector3 _startPoint; // Linecast 시작점
    private bool _isPooled; // 오브젝트 풀 사용 여부

    // Awake: 컴포넌트 캐싱 및 풀 생성 (PoolManager는 선택사항)
    protected override void Awake()
    {
        base.Awake(); // 부모 클래스의 Awake 호출 (Rigidbody 등 캐싱)
        rb = GetComponent<Rigidbody>();

        _isPooled = PoolManager.Instance != null;

        if (_isPooled)
        {
            if (decalPrefab != null) PoolManager.Instance.CreatePool(decalPrefab, 5);
            if (bloodPrefab != null) PoolManager.Instance.CreatePool(bloodPrefab, 5);
        }
    }

    // OnEnable: 오브젝트 풀에서 활성화될 때 호출
    private void OnEnable()
    {
        if (lifeTime > 0)
        {
            CancelInvoke(nameof(Deactivate)); // 기존 예약 취소
            if (_isPooled && PoolManager.Instance != null)
            {
                Invoke(nameof(Deactivate), lifeTime); // 풀 반환 예약
            }
            else
            {
                Destroy(gameObject, lifeTime); // 파괴 예약
            }
        }
    }

    // BulletStart: 총알 발사 시 초기 상태 설정
    public override void BulletStart(Transform bulletCreator)
    {
        base.BulletStart(bulletCreator);

        var weap = bulletCreator.GetComponent<Weapon>();
        if (weap != null)
        {
            force = weap.BulletForce;
            startSpeed = weap.BulletStartSpeed;
            this.damage = weap.PlayerDamage; // 부모의 damage 변수 설정
            // this.hittableTag는 부모 클래스(BulletBehaviour) 인스펙터에서 설정된 값 사용
        }
        else
        {
            Debug.LogError("Bullet Creator에 Weapon 컴포넌트가 없습니다!", bulletCreator);
            startSpeed = 50f; // 기본값
            this.damage = 10f; // 기본값
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(transform.forward * startSpeed, ForceMode.Impulse);
        }
        _startPoint = transform.position;
    }

    void Update()
    {
        Vector3 currentPosition = transform.position;

        // Linecast 수행 (ignoreMask 제외)
        if (Physics.Linecast(_startPoint, currentPosition, out RaycastHit hit, mask))
        {
            bool hitProcessed = false; // 충돌 처리 여부

            // --- 몬스터 피격 처리 ---
            // 1. 충돌 대상의 태그가 hittableTag(예: "Entity")와 일치하는지 확인
            if (hit.transform.CompareTag(hittableTag))
            {
                MonsterHealth monsterHealth = hit.transform.GetComponentInParent<MonsterHealth>();
                if (monsterHealth != null)
                {
                    // 2. TakeDamage 직접 호출!
                    monsterHealth.TakeDamage(this.damage);
                    Debug.Log($"Hit Monster '{hit.transform.name}' for {this.damage} damage.");

                    // 3. 피 효과 생성
                    if (bloodPrefab != null)
                    {
                        SpawnEffect(bloodPrefab, hit, 3f);
                    }
                    hitProcessed = true; // 몬스터 처리 완료
                }
                else
                {
                    Debug.LogWarning($"Object tagged '{hittableTag}' hit, but no MonsterHealth found in parents: {hit.transform.name}");
                    // 태그는 맞지만 MonsterHealth가 없는 경우 (예: 파괴 가능한 오브젝트?)
                    // 필요시 여기에 다른 로직 추가 가능
                    hitProcessed = true; // 태그가 맞으면 일단 처리된 것으로 간주 (총알 멈춤)
                }
            }
            // --- 플레이어 HitBox 처리 ---
            else if (hit.transform.CompareTag("HitBox"))
            {
                PlayerHealth playerHealth = hit.transform.root.GetComponentInChildren<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.SetDamage(30); // 데미지 값 수정 필요
                    if (bloodPrefab != null) { SpawnEffect(bloodPrefab, hit, 3f); } // 플레이어도 피?
                }
                hitProcessed = true;
            }
            // --- 환경 처리 ---
            else if (decalPrefab != null)
            {
                SpawnEffect(decalPrefab, hit, 15f);
                hitProcessed = true;
            }

            // Rigidbody에 힘 가하기
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForceAtPosition(transform.forward * force, hit.point, ForceMode.Impulse);
            }

            // 유효한 충돌 시 총알 비활성화
            if (hitProcessed)
            {
                Deactivate();
                return; // Update 종료
            }
        }

        // 다음 프레임 시작점 업데이트
        _startPoint = currentPosition;
    }

    private void SpawnEffect(GameObject prefab, RaycastHit hit, float effectLifetime)
    {
        if (prefab == null) return;

        GameObject effectGO;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        Vector3 position = hit.point + (hit.normal * 0.001f);

        if (_isPooled && PoolManager.Instance != null)
        {
            effectGO = PoolManager.Instance.Spawn(prefab, position, rotation);
            if (effectGO != null) PoolManager.Instance.ReturnToPool(effectGO, effectLifetime);
        }
        else
        {
            effectGO = Instantiate(prefab, position, rotation);
            Destroy(effectGO, effectLifetime);
        }
    }

    private void Deactivate()
    {
        if (_isPooled && PoolManager.Instance != null)
        {
            PoolManager.Instance.ReturnToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}