using UnityEngine;
using System;
using System.Collections.Generic;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    private int equipmentSlotCapacity = 13;
    public List<RelicData> equipmentSlots;

    public static event Action OnEquipmentChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
    /// 지정된 장비 슬롯에 아이템을 장착합니다.
    /// </summary>
    public bool EquipItem(RelicData itemToEquip, int inventorySlotIndex, int targetEquipSlotIndex)
    {
        InventoryManager.Instance.RemoveItemFromSlot(inventorySlotIndex, 1);

        int equipIndex = targetEquipSlotIndex;
        RelicData oldItem = equipmentSlots[equipIndex];

        if (oldItem != null)
        {
            bool addBackSuccess = InventoryManager.Instance.AddItem(oldItem);

            if (!addBackSuccess)
            {
                InventoryManager.Instance.AddItem(itemToEquip);
                InventoryManager.Instance.NotifyInventoryChanged();

                Debug.LogError("[EquipItem] 교체 시도 중, 기존 장비가 인벤토리에 들어갈 공간이 없어 장착 실패! 아이템이 복구되었습니다.");
                return false;
            }

            if (oldItem.grantedAbility != null)
            {
                PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
                if (playerAbilities != null)
                {
                    playerAbilities.RemoveRelic(oldItem.itemID);
                }
            }
        }

        equipmentSlots[equipIndex] = itemToEquip;

        if (itemToEquip.grantedAbility != null)
        {
            PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
            if (playerAbilities != null)
            {
                playerAbilities.AddRelic(itemToEquip.itemID);
            }
        }

        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 이 아이템을 장착할 수 있는 '첫 번째 빈 슬롯'을 찾아 장착합니다.
    /// </summary>
    public bool EquipItemToFirstAvailableSlot(RelicData itemToEquip, int inventorySlotIndex)
    {
        EquipmentSlot_UI[] allEquipSlots = FindObjectsOfType<EquipmentSlot_UI>();

        int targetEmptySlotIndex = -1;
        int targetFilledSlotIndex = -1;

        foreach (EquipmentSlot_UI slotUI in allEquipSlots)
        {
            if (slotUI.CanEquipItem(itemToEquip))
            {
                if (slotUI.currentItem == null)
                {
                    targetEmptySlotIndex = slotUI.equipmentSlotIndex;
                    break;
                }
                else if (targetFilledSlotIndex == -1)
                {
                    targetFilledSlotIndex = slotUI.equipmentSlotIndex;
                }
            }
        }

        if (targetEmptySlotIndex != -1)
        {
            return EquipItem(itemToEquip, inventorySlotIndex, targetEmptySlotIndex);
        }

        if (targetFilledSlotIndex != -1)
        {
            return EquipItem(itemToEquip, inventorySlotIndex, targetFilledSlotIndex);
        }

        Debug.Log($"이 아이템({itemToEquip.itemName})을 장착할 수 있는 슬롯이 아예 없습니다.");
        return false;
    }

    /// <summary>
    /// 장착된 아이템을 해제하여 인벤토리의 빈 공간으로 이동시킵니다.
    /// </summary>
    public bool UnequipItem(RelicData itemToUnequip, int equipSlotIndex)
    {
        bool success = InventoryManager.Instance.AddItem(itemToUnequip);

        if (!success)
        {
            Debug.Log("인벤토리가 꽉 차서 장비를 해제할 수 없습니다.");
            return false;
        }

        equipmentSlots[equipSlotIndex] = null;

        if (itemToUnequip.grantedAbility != null)
        {
            PlayerAbilityManager playerAbilities = FindObjectOfType<PlayerAbilityManager>();
            if (playerAbilities != null)
            {
                playerAbilities.RemoveRelic(itemToUnequip.itemID);
            }
        }

        OnEquipmentChanged?.Invoke();

        Debug.Log(itemToUnequip.itemName + "을(를) 장착 해제했습니다.");
        return true;
    }

    /// <summary>
    /// [추가] 두 장비 슬롯의 아이템을 교체합니다.
    /// </summary>
    public bool SwapEquipmentSlots(int slotIndexA, int slotIndexB)
    {
        if (slotIndexA < 0 || slotIndexA >= equipmentSlots.Count ||
            slotIndexB < 0 || slotIndexB >= equipmentSlots.Count)
        {
            Debug.LogError("[EquipmentManager] 잘못된 슬롯 인덱스");
            return false;
        }

        // 두 슬롯의 아이템을 서로 교환
        RelicData temp = equipmentSlots[slotIndexA];
        equipmentSlots[slotIndexA] = equipmentSlots[slotIndexB];
        equipmentSlots[slotIndexB] = temp;

        Debug.Log($"[장비 교체] 슬롯 {slotIndexA} ↔ 슬롯 {slotIndexB}");

        OnEquipmentChanged?.Invoke();
        return true;
    }
}