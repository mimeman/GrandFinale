// Assets/Monster_v1/Scripts/Items/ItemPickup.cs
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ItemPickup : MonoBehaviour
{
    [Header("이 아이템의 데이터")]
    [Tooltip("여기에 RelicData ScriptableObject를 끌어다 놓으세요.")]
    public RelicData itemData; 


    private void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true; 
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. 플레이어 태그 확인
        if (other.CompareTag("Player")) 
        {
            // 2. ItemData가 할당되었는지 확인
            if (itemData == null) 
            {
                Debug.LogWarning("ItemPickup에 itemData가 할당되지 않았습니다!", this);
                return;
            }

            // 3. 플레이어의 PlayerAbilityManager 찾기
            PlayerAbilityManager manager = other.GetComponentInParent<PlayerAbilityManager>();

            if (manager != null)
            {
                // (이 함수가 스탯 재계산을 트리거합니다)
                Debug.Log($"[ItemPickup] {itemData.itemName} 획득 시도...");
                manager.AddRelic(itemData.itemID);

                // 5. 픽업 아이템 파괴
                Destroy(gameObject); 
            }
            else
            {
                Debug.LogWarning($"플레이어에게 {itemData.itemName}를 획득할 PlayerAbilityManager가 없습니다.", other);
            }
        }
    }
}