// Assets/Scripts/Managers/PlayerAbilityManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WeaponController))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerAbilityManager : MonoBehaviour
{
    // --- 필수 컴포넌트 참조 ---
    private WeaponController weaponController;
    private PlayerStats playerStats;
    private CharacterMove characterMove;

    // --- 스킬 상태 관리 ---
    private Dictionary<string, Coroutine> runningSkillCoroutines = new Dictionary<string, Coroutine>();
    private Dictionary<string, bool> skillCooldowns = new Dictionary<string, bool>();

    //  1. (추가) 현재 보유한 유물(패시브) 리스트 
    private List<RelicData> equippedRelics = new List<RelicData>();
    // (Aura 같은 패시브 이펙트 관리를 위한 리스트)
    private List<GameObject> passiveEffectInstances = new List<GameObject>();


    void Awake()
    {
        // 필수 컴포넌트 찾아오기
        weaponController = GetComponent<WeaponController>();
        playerStats = GetComponent<PlayerStats>();
        characterMove = GetComponent<CharacterMove>();


        if (playerStats == null) Debug.LogError("PlayerStats가 없습니다. (패시브 적용 불가)");
    }



    // [테스트용] 키 입력
    void Update()
    {
        // ★★★ (추가) G/H 키로 아이템 추가/제거 테스트 ★★★
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("REL_001 실행");
            AddRelic("REL_001"); // (본인) 최대 체력 증가
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("REL_001 해제");
            RemoveRelic("REL_001"); // (본인) 최대 체력 증가 (제거)
        }

        // K키로 '무한 탄창' 발동 테스트
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("ABIL_007 실행");
            TryActivateAbility("ABIL_007");
        }

        // L키로 '에너지 실드' 발동 테스트
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("ABIL_006 실행");
            TryActivateAbility("ABIL_006");
        }
    }

    // ====================================================================
    // ★★★ 2. (신규) 아이템 추가 / 제거 (공용 함수) ★
    // ====================================================================

    /// <summary>
    /// (ItemPickup.cs가 호출)
    /// 플레이어에게 유물을 추가하고 스탯을 재계산합니다.
    /// </summary>
    public void AddRelic(string itemID)
    {
        if (!DataManager.Instance.RelicDB.TryGetValue(itemID, out RelicData relic))
        {
            Debug.LogWarning($"[AbilityManager] {itemID} RelicData를 찾을 수 없음");
            return;
        }

        // (중복 획득 방지 로직 등... )
        if (equippedRelics.Contains(relic))
        {
            equippedRelics.Add(relic);
            Debug.Log($"[AbilityManager] {relic.itemName}는(은) 이미 보유 중입니다.");
            return;
        }

        equippedRelics.Add(relic);
        Debug.Log($"[AbilityManager] {relic.itemName} 획득.");

        // 스탯 재계산!
        RecalculateAllPassiveStats();
    }

    /// <summary>
    /// (아이템 버리기 기능이 호출)
    /// 플레이어에게서 유물을 제거하고 스탯을 재계산합니다.
    /// </summary>
    public void RemoveRelic(string itemID)
    {
        if (!DataManager.Instance.RelicDB.TryGetValue(itemID, out RelicData relic)) return;

        if (equippedRelics.Remove(relic))
        {
            Debug.Log($"[AbilityManager] {relic.itemName} 제거.");
            // 스탯 재계산!
            RecalculateAllPassiveStats();
        }
    }

    // ====================================================================
    // ★★★ 3. (업그레이드) 스탯 재계산 로직 ★★★
    // (기존 ApplyPassiveAbility 함수를 대체)
    // ====================================================================

    /// <summary>
    /// 보유한 모든 유물(Relic)을 기반으로 패시브 스탯/능력을 처음부터 다시 적용합니다.
    /// </summary>
    private void RecalculateAllPassiveStats()
    {
        // --- 1. 기존 패시브 효과 모두 제거 ---
        StopAndClearAllPassiveEffects();

        // --- 2. PlayerStats를 기본값으로 리셋 ---
        if (playerStats == null) return;
        playerStats.ResetToBaseStats();

        // --- 3. 보유한 모든 유물을 순회하며 패시브 적용 ---
        foreach (RelicData relic in equippedRelics)
        {
            AbilityData ability = relic.grantedAbility;
            if (ability != null && ability.activationType == "Passive")
            {
                ApplyPassiveLogic(ability);
            }
        }

        // (필요시 체력 동기화 - 최대 체력이 줄었을 때 현재 체력이 더 높으면 안 됨)
        playerStats.ValidateHealth();
        // (PlayerStats.cs에 ValidateHealth() { if (CurrentHealth > CurrentMaxHealth) CurrentHealth = CurrentMaxHealth; } 추가 필요)
    }

    /// <summary>
    /// 패시브 능력 1개의 로직을 실제로 적용합니다.
    /// </summary>
    private void ApplyPassiveLogic(AbilityData ability)
    {
        if (playerStats == null) return;

        switch (ability.abilityLogicID)
        {
            case "Stat_Add":
                // Key="MaxHealth", ValueA="50"
                playerStats.AddStat(ability.param_Key, float.Parse(ability.param_ValueA));
                break;

            case "Stat_Percent":
                // Key="MoveSpeed", ValueA="10"
                playerStats.AddStatPercent(ability.param_Key, float.Parse(ability.param_ValueA));
                break;

            case "Aura_Heal_Ally":
            case "Aura_Damage_Enemy":
                Debug.Log($"{ability.abilityName} 오라 생성 (구현 필요)");
                if (!string.IsNullOrEmpty(ability.resourcePath))
                {
                    // GameObject prefab = ... (데이터에서 로드)
                    // GameObject instance = Instantiate(prefab, transform);
                    // passiveEffectInstances.Add(instance); // ★제거를 위해 리스트에 추가
                }
                break;

            default:
                Debug.LogWarning($"정의되지 않은 패시브 로직: {ability.abilityLogicID}");
                break;
        }
    }

    /// <summary>
    /// 스탯 재계산 전에 모든 오라/이펙트를 멈추고 파괴합니다.
    /// </summary>
    private void StopAndClearAllPassiveEffects()
    {
        foreach (GameObject instance in passiveEffectInstances)
        {
            if (instance != null) Destroy(instance);
        }
        passiveEffectInstances.Clear();
        // (이 외에 패시브 코루틴이 있다면 StopCoroutine...)
    }


    // ====================================================================
    // 4. 액티브 능력 발동 (기존 코드와 동일)
    // ====================================================================
    public void TryActivateAbility(string abilityID)
    {
        Debug.Log("ABIL_007 '무한탄창' 스킬 시작");


        // 1. 쿨타임 확인
        if (skillCooldowns.TryGetValue(abilityID, out bool onCooldown) && onCooldown)
        {
            Debug.Log($"[{abilityID}] 스킬 쿨타임 중입니다.");
            return;
        }

        // 2. 데이터 가져오기
        if (!DataManager.Instance.AbilityDB.TryGetValue(abilityID, out AbilityData ability))
        {
            Debug.LogWarning($"[{abilityID}] AbilityData를 찾을 수 없음.");
            return;
        }
        if (ability.activationType != "Active") return;

        // 3. 로직 ID에 따라 스킬 코루틴 실행
        IEnumerator skillRoutine = null;

        switch (ability.abilityLogicID)
        {
            case "Apply_Self_Buff":
                if (ability.param_Key == "InfiniteAmmo") // (ABIL_007)
                {
                    skillRoutine = InfiniteAmmoRoutine(ability);
                }
                break;

            case "Add_Shield": // (ABIL_006)
                // ★★★ (수정) PlayerStats의 함수를 직접 호출하도록 변경 ★★★
                float.TryParse(ability.param_Key, out float shieldAmount); // 100
                float.TryParse(ability.param_ValueA, out float duration); // 10
                float.TryParse(ability.param_ValueB, out float cooldown); // 20

                if (playerStats != null)
                {
                    playerStats.AddTemporaryShield(shieldAmount, duration); // ★ 실드 적용
                    StartCoroutine(CooldownRoutine(ability.abilityID, cooldown)); // ★ 쿨타임만 관리
                }
                break;

            case "Cone_Knockback": // (ABIL_005)
                // (넉백은 즉발성이므로 코루틴이 아닐 수도 있음)
                // ExecuteKnockback(ability);
                Debug.Log("넉백 스킬 발동 (구현 필요)");
                break;

            default:
                Debug.LogWarning($"정의되지 않은 액티브 로직: {ability.abilityLogicID}");
                break;
        }

        // 4. 선택된 스킬 코루틴 실행 (Add_Shield는 제외)
        if (skillRoutine != null)
        {
            StartSkillCoroutine(abilityID, skillRoutine);
        }
    }

    // ====================================================================
    // 5. 스킬 로직 (코루틴) (기존 코드와 동일)
    // ====================================================================

    /**
     * ABIL_007: (본인) 무한 탄창
     */
    private IEnumerator InfiniteAmmoRoutine(AbilityData ability)
    {
        // --- 1. 데이터 파싱 및 쿨타임 시작 ---
        float.TryParse(ability.param_ValueA, out float duration); // 10
        float.TryParse(ability.param_ValueB, out float cooldown); // 60
        StartCoroutine(CooldownRoutine(ability.abilityID, cooldown)); // 쿨타임 즉시 시작

        Debug.Log($"[{ability.abilityName}] 스킬 활성화! (지속: {duration}초)");
        // (이펙트 생성: ability.resourcePath)

        // --- 2. 스킬 지속 (핵심) ---
        float timer = 0f;
        while (timer < duration)
        {
            Weapon currentWeapon = weaponController.GETCurrentWeapon;

            if (currentWeapon != null)
            {
                currentWeapon.currentAmmo = currentWeapon.MaxAmmo;
            }

            timer += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // --- 3. 스킬 종료 ---
        Debug.Log($"[{ability.abilityName}] 스킬 종료.");
        // (이펙트 제거)
        runningSkillCoroutines.Remove(ability.abilityID);
    }

    /**
     * ABIL_006: (본인) 에너지 실드
     * (PlayerStats.AddTemporaryShield로 이동했으므로 이 코루틴은 이제 필요 없음)
     */
    // private IEnumerator AddShieldRoutine(AbilityData ability) { ... } // ★★★ 삭제됨 ★★★


    // ====================================================================
    // 6. 유틸리티 (코루틴 관리) (기존 코드와 동일)
    // ====================================================================

    // 스킬 코루틴 시작 (중복 방지)
    private void StartSkillCoroutine(string abilityID, IEnumerator routine)
    {
        if (runningSkillCoroutines.ContainsKey(abilityID))
        {
            StopCoroutine(runningSkillCoroutines[abilityID]);
            runningSkillCoroutines.Remove(abilityID);
        }
        runningSkillCoroutines.Add(abilityID, StartCoroutine(routine));
    }

    // 쿨타임 코루틴
    private IEnumerator CooldownRoutine(string abilityID, float cooldownTime)
    {
        skillCooldowns[abilityID] = true; // 쿨타임 시작
        Debug.Log($"[{abilityID}] 쿨타임 시작: {cooldownTime}초");

        yield return new WaitForSeconds(cooldownTime);

        skillCooldowns[abilityID] = false; // 쿨타임 종료
        Debug.Log($"[{abilityID}] 쿨타임 종료.");
    }
}