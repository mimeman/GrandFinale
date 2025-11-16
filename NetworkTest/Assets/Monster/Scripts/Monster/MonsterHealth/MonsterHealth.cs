// MonsterHealth.cs
using UnityEngine;
using UnityEngine.Events;

public class MonsterHealth : MonoBehaviour
{
    #region 필드
    [Header("아이템 드랍 설정")]
    [Tooltip("모든 아이템이 공용으로 사용할 'GenericLootDrop' 프리팹")]
    [SerializeField] private GameObject genericLootPrefab;



    public float _maxHP { get; private set; }
    private float _defense;

    [Space(10)]
    [Header("방어/반격 설정")]
    [Tooltip("이 시간(초) 안에")]
    public float blockTriggerTime = 2.0f;
    [Tooltip("이 횟수(번) 이상 피격 시")]
    public int blockTriggerHits = 5;
    [Tooltip("방어/반격 이벤트")]
    public UnityEvent OnBlock;

    public int hitCounter { get; private set; } = 0; // <-- 이렇게 변경
    private float hitTimer = 0f;

    [Header("체력 상태 (실시간)")]
    [SerializeField]
    [Tooltip("현재 체력 (실시간 디버그용)")]
    private float currentHP;

    public float CurrentHP
    {
        get { return currentHP; }
        private set { currentHP = value; }
    }

    public bool IsDead { get; private set; }

    public UnityEvent OnHit;
    public UnityEvent OnDeath;

    // ★ 1. (추가) AI 컨트롤러 참조
    private MonsterAIController ai;

    [Space(10)]
    [Header("--- DEBUG TOOLS ---")]
    public bool _DEBUG_ForceHit = false;
    public bool _DEBUG_ForceDie = false;
    #endregion

    private void Awake()
    {
        ai = GetComponent<MonsterAIController>();
    }

    private void Update()
    {
        if (_DEBUG_ForceDie) { }
        else if (_DEBUG_ForceHit) {  }

        // (Block 타이머 로직은 그대로)
        if (hitTimer > 0)
        {
            hitTimer -= Time.deltaTime;
            if (hitTimer <= 0)
            {
                hitCounter = 0;
            }
        }
    }

    public void Initialize(MonsterConfig config)
    {
        _maxHP = config.maxHP;
        _defense = config.defense;
        currentHP = _maxHP;
        IsDead = false;
    }

    /// <summary>
    /// 외부로부터 데미지를 받는 함수입니다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        float actualDamage = 0f;
        float currentDefense = 0f; // 기본 방어력은 0

        if (ai != null && ai.fsm is GolemFSM && ai.CurrentState == ai.fsm.BlockState)
        {
            // Block 상태라면, 현재 '페이즈'를 가져옵니다.
            var blockState = ai.CurrentState as GolemStates.Block;
            if (blockState != null && blockState.CurrentPhase == GolemStates.Block.Phase.Blocking)
            {
                // "방어 중" 페이즈일 때만 설정된 방어력(_defense)을 사용합니다.
                currentDefense = _defense;
            }
            // (else: VulnerableCheck 페이즈나 CounterRush 페이즈일때는 방어력 0)
        }


        // 2. 최종 데미지 계산 (기존 로직)
        actualDamage = Mathf.Max(damage - currentDefense, 0f);
        currentHP -= actualDamage;


        if (currentHP <= 0)
        {
            currentHP = 0;
            IsDead = true;
            OnDeath?.Invoke();
            Debug.Log("<color=red>사망 신호 발생!</color>");
        }
        else
        {
            // (살아있을 때 피격 당함)
            OnHit?.Invoke();
            Debug.Log("<color=yellow>피격 신호 발생!</color>");

            // (골렘 방어 카운터 로직 - 기존과 동일)
            if (ai != null && ai.fsm is GolemFSM)
            {
                if (hitTimer <= 0)
                {
                    hitTimer = blockTriggerTime;
                    hitCounter = 1;
                }
                else
                {
                    hitCounter++;
                }

                if (hitCounter >= blockTriggerHits)
                {
                    Debug.Log($"[{gameObject.name}] 방어/반격 발동!");
                    OnBlock?.Invoke();
                    hitTimer = 0;
                    hitCounter = 0;
                    return;
                }
            }
        }
    }

    public void SpawnLoot(LootTable lootTable)
    {
        if (lootTable == null || lootTable.items == null)
        {
            Debug.LogWarning("LootTable이 비어있습니다.", this);
            return;
        }
        if (genericLootPrefab == null)
        {
            Debug.LogError($"[드랍 실패] {gameObject.name}에 genericLootPrefab이 연결되지 않았습니다!", this);
            return;
        }

        float groundY = transform.position.y; // 기본값은 몬스터 피벗의 Y
        RaycastHit hit;

        // 몬스터의 위치에서 아래로 100m 레이캐스트를 쏴서 지형을 찾습니다.
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 100f))
        {
            // 레이가 맞은 지점의 Y 좌표를 사용합니다.
            groundY = hit.point.y;
            Debug.Log($"[Raycast] 지면 찾음: Y = {groundY}");
        }
        else
        {
            Debug.LogWarning("지면을 찾지 못했습니다. 아이템이 공중에 생성될 수 있습니다.");
        }


        float scatterDistance = 1.0f; // 아이템을 흩뿌릴 범위

        foreach (var entry in lootTable.items)
        {
            if (entry.item == null) continue;

            if (Random.Range(0f, 100f) <= entry.dropChance)
            {
                GameObject prefabToSpawn = genericLootPrefab;

                // 2. 랜덤 오프셋 계산 (흩뿌림)
                Vector2 randomCircle = Random.insideUnitCircle * scatterDistance;

                // 3. 생성 위치를 레이캐스트로 찾은 지면(groundY)으로 고정합니다.
                Vector3 spawnPos = transform.position;
                spawnPos.x += randomCircle.x;
                spawnPos.z += randomCircle.y;

                spawnPos.y = groundY + 0.5f;
                GameObject spawnedItem = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);


                // 4. 생성된 구체에 데이터 주입
                ItemPickup pickupScript = spawnedItem.GetComponent<ItemPickup>();
                if (pickupScript != null)
                {
                    pickupScript.itemData = entry.item;
                    pickupScript.addToInventoryInstead = true;
                }

                // 5. VFX 스크립트에 등급 주입 (VFX 위치 제어는 LootOrbVisuals가 전담)
                LootOrbVisuals visualScript = spawnedItem.GetComponent<LootOrbVisuals>();
                if (visualScript != null)
                {
                    visualScript.Initialize(entry.item.grade);
                }
                else
                {
                    Debug.LogError("[LootSpawn ERROR] GenericLootDrop 프리팹에 LootOrbVisuals.cs가 없습니다!");
                }
            }
        }
    }
}
