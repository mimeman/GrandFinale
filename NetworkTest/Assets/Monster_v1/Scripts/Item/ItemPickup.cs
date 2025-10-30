// ItemPickup.cs
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("이 아이템의 데이터")]
    [Tooltip("여기에 RelicData ScriptableObject를 끌어다 놓으세요.")]
    public RelicData itemData;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (itemData == null) return; // 데이터가 없으면 실행 안 함

            Debug.Log(itemData.itemName + "을(를) 획득!");

            // ★★★ RelicData의 AbilityData를 사용하는 로직 (예시) ★★★
            ApplyRelicEffect(other.gameObject, itemData.grantedAbility);

            Destroy(gameObject);
        }
    }

    // RelicData/AbilityData를 처리하는 새 함수 (예시)
    private void ApplyRelicEffect(GameObject player, AbilityData ability)
    {
        if (ability == null)
        {
            Debug.Log($"[{itemData.itemName}] 획득. 특별한 능력 없음.");
            return;
        }

        Debug.Log($"[{itemData.itemName}] 획득. 능력: {ability.abilityName} 적용!");

        // 예: 능력 로직 ID에 따라 플레이어 스탯 변경
        if (ability.abilityLogicID == "Stat_Add" && ability.param_Key == "MaxHealth")
        {
            // PlayerStats stats = player.GetComponent<PlayerStats>();
            // if (stats != null)
            // {
            //     float healthBonus = float.Parse(ability.param_ValueA); // "25" -> 25f
            //     stats.AddMaxHealth(healthBonus);
            // }
            Debug.Log($"임시 효과: 최대 체력 +{ability.param_ValueA}");
        }
        else if (ability.abilityLogicID == "Projectile")
        {
            // PlayerSkillManager skills = player.GetComponent<PlayerSkillManager>();
            // if (skills != null)
            // {
            //     skills.EquipSkill(ability); // AbilityData를 장착
            // }
            Debug.Log($"임시 효과: {ability.abilityName} 스킬 획득!");
        }
    }
}