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

    public bool EquipItemToFirstAvailableSlot(RelicData itemToEquip, int inventorySlotIndex)
    {
        EquipmentSlot_UI[] allEquipSlots = FindObjectsOfType<EquipmentSlot_UI>(true);

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

        return false;
    }

    public bool UnequipItem(RelicData itemToUnequip, int equipSlotIndex)
    {
        bool success = InventoryManager.Instance.AddItem(itemToUnequip);

        if (!success)
        {
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
        return true;
    }

    public bool SwapEquipmentSlots(int slotIndexA, int slotIndexB)
    {
        if (slotIndexA < 0 || slotIndexA >= equipmentSlots.Count ||
            slotIndexB < 0 || slotIndexB >= equipmentSlots.Count)
        {
            return false;
        }

        RelicData temp = equipmentSlots[slotIndexA];
        equipmentSlots[slotIndexA] = equipmentSlots[slotIndexB];
        equipmentSlots[slotIndexB] = temp;

        OnEquipmentChanged?.Invoke();
        return true;
    }
}