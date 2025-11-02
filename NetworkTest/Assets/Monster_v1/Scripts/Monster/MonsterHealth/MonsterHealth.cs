// MonsterHealth.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 몬스터의 체력, 피격, 사망을 관리하는 재사용 가능한 컴포넌트입니다.
/// </summary>
public class MonsterHealth : MonoBehaviour
{
    #region 필드
    [Header("아이템 드랍 설정")]
    [Tooltip("몬스터가 죽었을 때 떨어뜨릴 아이템 프리팹 (ItemPickup 스크립트 포함)")]
    public GameObject itemPickupPrefab;


    // Config에서 값을 받아와 저장할 private 변수
    private float _maxHP;
    private float _defense;

    [Header("체력 상태 (실시간)")]
    [SerializeField]
    [Tooltip("현재 체력 (실시간 디버그용)")]
    private float currentHP; // private 필드로 변경

    public float CurrentHP // public 프로퍼티는 그대로 둡니다.
    {
        get { return currentHP; }
        private set { currentHP = value; } // private set도 그대로
    }

    public bool IsDead { get; private set; }

    // --- 외부 신호(Event) ---
    public UnityEvent OnHit;
    public UnityEvent OnDeath;

    [Space(10)]
    [Header("--- DEBUG TOOLS ---")]
    [Tooltip("이 체크박스를 누르면 몬스터에게 10의 데미지를 줍니다.")]
    public bool _DEBUG_ForceHit = false;
    [Tooltip("이 체크박스를 누르면 몬스터를 즉시 사망시킵니다.")]
    public bool _DEBUG_ForceDie = false;

    #endregion

    private void Awake()
    {
        MonsterAIController ai = GetComponent<MonsterAIController>();
    }

    private void Update()
    {
        if (_DEBUG_ForceDie)
        {
            _DEBUG_ForceDie = false;
            if (!IsDead)
            {
                Debug.LogWarning($"[{gameObject.name}] DEBUG: 강제 사망 신호!");
                TakeDamage(currentHP + _defense);
            }
        }
        else if (_DEBUG_ForceHit)
        {
            _DEBUG_ForceHit = false;
            if (!IsDead)
            {
                Debug.LogWarning($"[{gameObject.name}] DEBUG: 강제 피격 신호! (데미지 10)");
                TakeDamage(10f);
            }
        }
    }

    /// <summary>
    /// MonsterConfig의 설정값으로 체력 컴포넌트를 초기화합니다.
    /// </summary>
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

        float actualDamage = Mathf.Max(damage - _defense, 0f);

        currentHP -= actualDamage;
        Debug.Log($"<color=orange>[{gameObject.name}] 피해! (입힌 데미지: {damage}, 방어력: {_defense}, 실제 피해: {actualDamage}) -> 현재 체력: {currentHP}/{_maxHP}</color>");

        if (currentHP <= 0)
        {
            currentHP = 0; // 체력이 음수가 되지 않도록
            IsDead = true;
            OnDeath?.Invoke();
            Debug.Log("<color=red>사망 신호 발생!</color>");

/*            MonsterAIController ai = GetComponent<MonsterAIController>();

            if (ai != null && ai.config.lootTable != null) { SpawnLoot(ai.config.lootTable); }

            if (MonsterManager.Instance != null) { MonsterManager.Instance.RegisterMonsterDied(); }*/
  
        }
        else
        {
            OnHit?.Invoke();
            Debug.Log("<color=yellow>피격 신호 발생!</color>");
        }
    }

    public void SpawnLoot(LootTable lootTable)
    {
        if (itemPickupPrefab == null) // <-- 대신 이 변수를 체크
        {
            Debug.LogError("MonsterHealth 스크립트에 ItemPickup 프리팹이 할당되지 않았습니다!", this);
            return;
        }

        foreach (var entry in lootTable.items)
        {
            if (Random.Range(0f, 100f) <= entry.dropChance)
            {
                GameObject spawnedItem = Instantiate(itemPickupPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
                spawnedItem.GetComponent<ItemPickup>().itemData = entry.item;
                Debug.Log($"{entry.item.itemName} 드랍! (확률: {entry.dropChance}%)");
            }
        }
    }
}