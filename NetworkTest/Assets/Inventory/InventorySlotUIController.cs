using UnityEngine;
using System.Collections.Generic;

public class InventorySlotUIController : MonoBehaviour
{
    [Header("인벤토리의 모든 슬롯 UI")]
    [SerializeField] private List<Slot_UI> allInventorySlots;

    [Header("자동 설정")]
    [SerializeField] private bool autoFindSlots = false;

    void Start()
    {
        if (autoFindSlots || allInventorySlots == null || allInventorySlots.Count == 0)
        {
            AutoFindSlots();
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged += UpdateSlotUIBindings;
        }

        UpdateSlotUIBindings();
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.OnInventoryChanged -= UpdateSlotUIBindings;
        }
    }

    private void AutoFindSlots()
    {
        Slot_UI[] foundSlots = GetComponentsInChildren<Slot_UI>(true);
        allInventorySlots = new List<Slot_UI>(foundSlots);
    }

    private void UpdateSlotUIBindings()
    {
        if (InventoryManager.Instance == null || allInventorySlots == null || allInventorySlots.Count == 0)
            return;

        List<InventorySlot> filteredSlots = InventoryManager.Instance.GetFilteredInventory();
        List<InventorySlot> originalSlots = InventoryManager.Instance.inventorySlots;

        for (int i = 0; i < allInventorySlots.Count; i++)
        {
            Slot_UI slotUI = allInventorySlots[i];
            if (slotUI == null) continue;

            InventorySlot slotData;

            if (i < filteredSlots.Count)
            {
                InventorySlot filteredSlot = filteredSlots[i];

                if (filteredSlot.slotIndex == -1)
                {
                    slotData = filteredSlot;
                }
                else
                {
                    slotData = originalSlots[filteredSlot.slotIndex];
                }
            }
            else
            {
                slotData = new InventorySlot() { slotIndex = -1 };
            }

            slotUI.SetBoundItem(slotData);
            slotUI.gameObject.SetActive(true);
        }
    }
}