using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("이 아이템의 데이터")]
    [Tooltip("여기에 아이템 ScriptableObject를 끌어다 놓으세요.")]
    public ItemData itemData;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 플레이어만 주울 수 있음
        if (other.CompareTag("Player"))
        {
            // 2. 플레이어의 인벤토리나 아이템 관리자에게 아이템 전달
            // (지금은 인벤토리가 없으니, 플레이어의 스탯/스킬 시스템에 바로 전달)
            Debug.Log(itemData.itemName + "을(를) 획득!");
            ApplyItemEffect(other.gameObject);

            // 3. 줍고 나면 아이템 파괴
            Destroy(gameObject);
        }
    }

    // "인벤토리 시스템 : 일단 대기" 요청을 반영한 임시 함수.
    // 인벤토리가 생기면 이 함수는 "AddItemToInventory"로 바뀌어야 합니다.
    private void ApplyItemEffect(GameObject player)
    {
        // 1. 스탯 아이템인지 확인
        if (itemData is StatBoostItemData statItem)
        {
            // (예시) 플레이어의 스탯 시스템을 찾아 스탯 적용
            // PlayerStats stats = player.GetComponent<PlayerStats>();
            // if (stats != null)
            // {
            //     stats.AddAttack(statItem.attackPowerIncrease);
            //     stats.AddHealth(statItem.maxHealthIncrease);
            // }
            Debug.Log($"임시 효과: 공격력 +{statItem.attackPowerIncrease}");
        }
        // 2. 스킬 아이템인지 확인
        else if (itemData is SkillItemData skillItem)
        {
            // (예시) 플레이어의 스킬 시스템을 찾아 스킬 장착
            // PlayerSkillManager skills = player.GetComponent<PlayerSkillManager>();
            // if (skills != null)
            // {
            //     skills.EquipSkill(skillItem);
            // }
            Debug.Log($"임시 효과: {skillItem.itemName} 스킬 획득!");
        }
    }
}