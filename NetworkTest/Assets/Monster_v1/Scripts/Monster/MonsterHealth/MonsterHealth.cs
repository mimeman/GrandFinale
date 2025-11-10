// MonsterHealth.cs
using UnityEngine;
using UnityEngine.Events;

public class MonsterHealth : MonoBehaviour
{
    #region 필드
    [Header("아이템 드랍 설정")]
    [Tooltip("몬스터가 죽었을 때 떨어뜨릴 아이템 프리팹 (ItemPickup 스크립트 포함)")]
    public GameObject itemPickupPrefab;

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
        // ★ 2. (수정) AI 컨트롤러 참조 저장
        ai = GetComponent<MonsterAIController>();
    }

    private void Update()
    {
        // ... (Debug 로직은 그대로) ...
        if (_DEBUG_ForceDie) { /* ... */ }
        else if (_DEBUG_ForceHit) { /* ... */ }

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
        Debug.Log($"[{gameObject.name}] Health 초기화 완료: HP={_maxHP}, DEF={_defense}");
    }

    /// <summary>
    /// 외부로부터 데미지를 받는 함수입니다.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        float actualDamage = 0f;
        float currentDefense = 0f; // 기본 방어력은 0

        // --- ★ 1. (수정) 골렘 방어 상태인지 체크 ---
        if (ai != null && ai.fsm is GolemFSM && ai.CurrentState == ai.fsm.BlockState)
        {
            // Block 상태라면, 현재 '페이즈'를 가져옵니다.
            var blockState = ai.CurrentState as GolemStates.Block;
            if (blockState != null && blockState.CurrentPhase == GolemStates.Block.Phase.Blocking)
            {
                // "방어 중" 페이즈일 때만 설정된 방어력(_defense)을 사용합니다.
                currentDefense = _defense;
                Debug.Log("GOLEM BLOCK: 방어 성공! 방어력 " + currentDefense + " 적용.");
            }
            // (else: VulnerableCheck 페이즈나 CounterRush 페이즈일때는 방어력 0)
        }
        // --- (Gazer나 다른 몬스터는 항상 방어력 0, 또는 기본 _defense값을 쓰게 하려면
        //    else { currentDefense = _defense; } 를 추가하세요) ---


        // 2. 최종 데미지 계산 (기존 로직)
        actualDamage = Mathf.Max(damage - currentDefense, 0f);
        currentHP -= actualDamage;

        // (디버그 로그 수정)
        Debug.Log($"<color=orange>[{gameObject.name}] 피해! (입힌 데미지: {damage}, 현재 방어력: {currentDefense}, 실제 피해: {actualDamage}) -> 현재 체력: {currentHP}/{_maxHP}</color>");

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

        foreach (var entry in lootTable.items)
        {
            if (entry.item == null)
            {
                Debug.LogWarning("LootTable에 비어있는 아이템 슬롯이 있습니다.", this);
                continue;
            }

            // 1. 드랍 확률 체크 (LootTable.cs 기반)
            if (Random.Range(0f, 100f) <= entry.dropChance)
            {
                // 2. RelicData에서 드랍 프리팹 가져오기 (RelicData.cs 기반)
                GameObject prefabToSpawn = entry.item.dropPrefab;
                if (prefabToSpawn != null)
                {
                    // 3. 몬스터 위치 (공중일 수 있음)에 생성
                    //    -> Rigidbody가 중력으로 떨어뜨릴 것입니다.
                    Vector3 spawnPos = transform.position + Vector3.up * 1f;
                    GameObject spawnedItem = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

                    Debug.Log($"{entry.item.itemName} 드랍! (확률: {entry.dropChance}%)");
                }
                else
                {
                    Debug.LogWarning($"{entry.item.itemName}은(는) 드랍되었지만, RelicData에 dropPrefab이 할당되지 않았습니다.", this);
                }
            }
        }
    }
}
