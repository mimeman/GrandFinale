using UnityEngine;
using System;
using System.Collections.Generic; // List 사용

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    // 1. RelicData를 담을 4개의 장비 슬롯
    private int equipmentSlotCapacity = 5;
    public List<RelicData> equipmentSlots;

    // 장비가 변경될 때 UI에 보낼 신호
    public static event Action OnEquipmentChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 2. RelicData 리스트로 4칸 초기화
        equipmentSlots = new List<RelicData>();
        for (int i = 0; i < equipmentSlotCapacity; i++)
        {
            equipmentSlots.Add(null);
        }
    }

    /// <summary>
    /// 아이템을 장착합니다. (Inventory -> Equipment)
    /// </summary>
    /// <param name="itemToEquip">장착할 RelicData</param>
    /// <param name="inventorySlotIndex">아이템이 원래 있던 인벤토리 슬롯 번호</param>
    /// <returns>장착 성공 여부</returns>
    public bool EquipItem(RelicData itemToEquip, int inventorySlotIndex) // 3. RelicData
    {
        // 4. 빈 장비 슬롯(0~3)을 찾습니다.
        int emptyEquipSlot = FindNextEmptyEquipSlot();

        if (emptyEquipSlot == -1)
        {
            Debug.Log("장비 슬롯이 꽉 찼습니다.");
            return false; // 장착 실패
        }

        // 5. 장비 슬롯에 아이템을 등록합니다.
        equipmentSlots[emptyEquipSlot] = itemToEquip;

        // 6. (중요) 가방(InventoryManager)에서 이 아이템을 제거합니다.
        InventoryManager.Instance.RemoveItem(inventorySlotIndex);

        // 7. (플레이어 스탯 적용 로직 - PlayerAbilityManager 호출)
        // 이 아이템의 능력을 플레이어에게 적용합니다.
        if (itemToEquip.grantedAbility != null)
        {
            // PlayerAbilityManager가 있는지 확인하고 능력을 추가합니다.
            // (ItemPickup.cs와 동일한 로직)
            PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>(); // (임시)
            if (playerAbilities != null)
            {
                playerAbilities.AddRelic(itemToEquip.itemID);
            }
        }

        // 8. 장비가 바뀌었다고 신호를 보냅니다.
        OnEquipmentChanged?.Invoke();

        Debug.Log(itemToEquip.itemName + "을(를) 장착했습니다.");
        return true;
    }

    // 빈 장비 슬롯을 0번부터 찾는 함수
    private int FindNextEmptyEquipSlot()
    {
        for (int i = 0; i < equipmentSlotCapacity; i++)
        {
            if (equipmentSlots[i] == null)
            {
                return i;
            }
        }
        return -1; // 꽉 찼음
    }

    public bool UnequipItem(RelicData itemToUnequip, int equipSlotIndex)
    {
        // 1. InventoryManager의 AddItem 함수를 호출합니다.
        // (이 함수가 알아서 빈 슬롯을 찾고, 꽉 찼는지 확인하며, OnInventoryChanged 이벤트도 호출합니다)
        bool success = InventoryManager.Instance.AddItem(itemToUnequip);

        // 2. AddItem이 실패했다면 (인벤토리 꽉 참)
        if (!success)
        {
            Debug.Log("인벤토리가 꽉 차서 장비를 해제할 수 없습니다.");
            return false; // 해제 실패
        }

        // 3. AddItem이 성공했다면, 장비 슬롯에서 이 아이템을 제거
        equipmentSlots[equipSlotIndex] = null;

        // 4. (플레이어 스탯 적용 해제 - PlayerAbilityManager 호출)
        if (itemToUnequip.grantedAbility != null)
        {
            PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
            if (playerAbilities != null)
            {
                // playerAbilities에 "RemoveRelic" 함수가 있다고 가정
                playerAbilities.RemoveRelic(itemToUnequip.itemID);
            }
        }

        // 5. '장비' UI에만 신호를 보냄 (인벤토리 신호는 AddItem이 알아서 보냄)
        OnEquipmentChanged?.Invoke();

        Debug.Log(itemToUnequip.itemName + "을(를) 장착 해제했습니다.");
        return true;
    }
}