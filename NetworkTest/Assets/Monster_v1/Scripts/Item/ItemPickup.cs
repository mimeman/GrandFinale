using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ItemPickup : MonoBehaviour
{
    [Header("이 아이템의 데이터")]
    [Tooltip("여기에 RelicData ScriptableObject를 끌어다 놓으세요.")]
    public RelicData itemData;

    [Header("픽업 방식 설정")]
    [Tooltip("체크하면 8칸 인벤토리로, 체크 해제하면 PlayerAbilityManager로 즉시 등록됩니다.")]
    public bool addToInventoryInstead = false; // 기본값은 '즉시 등록'
    // ------------------------------------

    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (itemData == null)
        {
            Debug.LogWarning("ItemPickup에 itemData가 할당되지 않았습니다!", this);
            return;
        }

        // 2-1. [인벤토리로 보내기]가 체크되어 있다면
        if (addToInventoryInstead)
        {
            // InventoryManager에 아이템 추가를 시도
            bool success = InventoryManager.Instance.AddItem(itemData);

            if (success)
            {
                Destroy(gameObject); // 줍기 성공!
            }
            else
            {
                Debug.Log("인벤토리가 꽉 찼습니다!"); // 줍기 실패 (꽉 참)
            }
        }
        // 2-2. [인벤토리로 보내기]가 체크 해제되어 있다면 (기존 로직)
        else
        {
            // PlayerAbilityManager를 찾아서 즉시 등록
            PlayerAbilityManager manager = other.GetComponentInParent<PlayerAbilityManager>();
            if (manager != null)
            {
                manager.AddRelic(itemData.itemID);
                Destroy(gameObject); // 줍기 성공!
            }
            else
            {
                Debug.LogWarning($"플레이어에게 {itemData.itemName}를 획득할 PlayerAbilityManager가 없습니다.", other);
            }
        }
    }
}