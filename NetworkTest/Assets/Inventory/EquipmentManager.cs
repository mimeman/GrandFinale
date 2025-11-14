using UnityEngine;
using System;
using System.Collections.Generic; // List 사용

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    // 1. RelicData를 담을 장비 슬롯
    // [수정] 13칸으로 늘어난 것을 확인했습니다.
    private int equipmentSlotCapacity = 13;
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

        equipmentSlots = new List<RelicData>();
        for (int i = 0; i < equipmentSlotCapacity; i++)
        {
            equipmentSlots.Add(null);
        }
    }

    /// <summary>
    /// (드래그 앤 드롭 또는 교체 시 사용)
    /// 지정된 장비 슬롯(targetEquipSlotIndex)에 아이템을 장착합니다.
    /// </summary>
    public bool EquipItem(RelicData itemToEquip, int inventorySlotIndex, int targetEquipSlotIndex)
    {
        // 1. 인벤토리에서 아이템 제거
        // [수정] RemoveItemFromSlot을 사용하여 수량 1개만 제거 시도
        InventoryManager.Instance.RemoveItemFromSlot(inventorySlotIndex, 1);

        // (참고: RemoveItemFromSlot은 수량이 0이 되면 자동으로 ClearSlot을 호출함)
        // (아이템이 완전히 제거되었는지 여부와 관계없이 장착 로직은 진행될 수 있음 - 스왑이므로)

        // 2. targetEquipSlotIndex를 장착 인덱스로 사용
        int equipIndex = targetEquipSlotIndex;
        RelicData oldItem = equipmentSlots[equipIndex]; // 덮어쓰기 전에 기존 아이템 저장

        // 3. 만약 'oldItem' (교체될 아이템)이 있었다면
        if (oldItem != null)
        {
            // 3-1. 기존 아이템(oldItem)을 인벤토리로 되돌림
            bool addBackSuccess = InventoryManager.Instance.AddItem(oldItem);

            if (!addBackSuccess)
            {
                // (롤백) 인벤토리가 꽉 차서 실패하면, 제거했던 아이템을 복구
                // AddItem은 스택을 시도하므로, 원본 슬롯에 다시 AddItem을 시도하는 것이 안전합니다.
                InventoryManager.Instance.AddItem(itemToEquip);
                InventoryManager.Instance.NotifyInventoryChanged(); // (AddItem이 호출하므로 중복일 수 있으나 안전을 위해 호출)

                Debug.LogError("[EquipItem] 교체 시도 중, 기존 장비가 인벤토리에 들어갈 공간이 없어 장착 실패! 아이템이 복구되었습니다.");
                return false;
            }

            // 3-2. 기존 아이템의 능력치 해제
            if (oldItem.grantedAbility != null)
            {
                PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
                if (playerAbilities != null)
                {
                    playerAbilities.RemoveRelic(oldItem.itemID);
                }
            }
        }

        // 4. 새 아이템 장착 (빈 슬롯이거나 교체된 슬롯)
        equipmentSlots[equipIndex] = itemToEquip;

        // 5. 새 아이템의 능력치 적용
        if (itemToEquip.grantedAbility != null)
        {
            PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
            if (playerAbilities != null)
            {
                playerAbilities.AddRelic(itemToEquip.itemID);
            }
        }

        // 6. 장비 변경 이벤트 알림
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// (더블클릭/우클릭 시 사용)
    /// 이 아이템을 장착할 수 있는 '첫 번째 빈 슬롯'을 찾아 장착합니다.
    /// </summary>
    public bool EquipItemToFirstAvailableSlot(RelicData itemToEquip, int inventorySlotIndex)
    {
        EquipmentSlot_UI[] allEquipSlots = FindObjectsOfType<EquipmentSlot_UI>();

        int targetEmptySlotIndex = -1;  // 1순위: 비어있는 슬롯
        int targetFilledSlotIndex = -1; // 2순위: 채워져있는 슬롯 (교체용)

        foreach (EquipmentSlot_UI slotUI in allEquipSlots)
        {
            // 이 슬롯이 아이템을 받을 수 있는지 먼저 검사
            if (slotUI.CanEquipItem(itemToEquip))
            {
                if (slotUI.currentItem == null)
                {
                    // 1. (1순위 발견) 비어있는 슬롯을 찾음
                    targetEmptySlotIndex = slotUI.equipmentSlotIndex;
                    break; // 가장 좋은 케이스이므로 즉시 반복 중단
                }
                else if (targetFilledSlotIndex == -1)
                {
                    // 2. (2순위 발견) 비어있진 않지만, 교체 가능한 '첫 번째' 슬롯을 저장
                    // (혹시 뒤에 빈 슬롯이 있을 수 있으니 break하지 않고 계속 탐색)
                    targetFilledSlotIndex = slotUI.equipmentSlotIndex;
                }
            }
        }

        // 3. (결과) 1순위인 '빈 슬롯'을 찾았다면, 거기에 장착
        if (targetEmptySlotIndex != -1)
        {
            return EquipItem(itemToEquip, inventorySlotIndex, targetEmptySlotIndex);
        }

        // 4. (결과) 빈 슬롯은 없었지만, '교체할 슬롯'을 찾았다면, 해당 슬롯과 교체
        if (targetFilledSlotIndex != -1)
        {
            return EquipItem(itemToEquip, inventorySlotIndex, targetFilledSlotIndex);
        }

        // 5. (결과) 이 아이템을 장착할 수 있는 슬롯이 아예 없음
        Debug.Log($"이 아이템({itemToEquip.itemName})을 장착할 수 있는 슬롯이 아예 없습니다.");
        return false;
    }

    /// <summary>
    /// 장착된 아이템을 해제하여 인벤토리의 빈 공간으로 이동시킵니다.
    /// </summary>
    public bool UnequipItem(RelicData itemToUnequip, int equipSlotIndex)
    {
        // 1. InventoryManager의 AddItem 함수를 호출합니다.
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
                playerAbilities.RemoveRelic(itemToUnequip.itemID);
            }
        }

        // 5. '장비' UI에만 신호를 보냄 (인벤토리 신호는 AddItem이 알아서 보냄)
        OnEquipmentChanged?.Invoke();

        Debug.Log(itemToUnequip.itemName + "을(를) 장착 해제했습니다.");
        return true;
    }
}