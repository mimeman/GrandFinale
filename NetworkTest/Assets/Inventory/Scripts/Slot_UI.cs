using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class Slot_UI : MonoBehaviour, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public Image slotIcon;
    public TextMeshProUGUI countText;

    public bool dropSuccessful = false;

    private Coroutine hideTooltipCoroutine;
    private Coroutine tooltipCoroutine;
    private const float TooltipDelay = 0.5f;

    public InventorySlot currentSlot { get; private set; }
    private Coroutine singleClickCoroutine;

    private static readonly string[] EquippableTypes =
    {
        ItemType.Weapon.ToString().ToLower(),
        ItemType.Artifact.ToString().ToLower(),
        ItemType.Accessory.ToString().ToLower(),
        ItemType.Equipment.ToString().ToLower()
    };

    public void SetBoundItem(InventorySlot newSlot)
    {
        currentSlot = newSlot;
        UpdateSlotVisuals();
    }

    void UpdateSlotVisuals()
    {
        if (slotIcon == null) return;

        if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1)
        {
            RelicData item = currentSlot.item;
            Sprite icon = Resources.Load<Sprite>(item.iconPath);

            if (icon != null)
            {
                slotIcon.sprite = icon;
                slotIcon.enabled = true;
            }
            else
            {
                slotIcon.enabled = false;
            }

            if (countText != null)
            {
                countText.text = currentSlot.quantity.ToString();
                countText.gameObject.SetActive(true);
                countText.fontSize = 10;
            }
        }
        else
        {
            slotIcon.sprite = null;
            slotIcon.enabled = false;

            if (countText != null)
            {
                countText.gameObject.SetActive(false);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.item == null || currentSlot.slotIndex == -1) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            AttemptEquip();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount == 1)
            {
                if (singleClickCoroutine != null)
                    StopCoroutine(singleClickCoroutine);

                if (IsMainInventoryView())
                    singleClickCoroutine = StartCoroutine(HandleSingleClick());
            }
            else if (eventData.clickCount == 2)
            {
                if (singleClickCoroutine != null)
                {
                    StopCoroutine(singleClickCoroutine);
                    singleClickCoroutine = null;
                }

                AttemptEquip();

                if (InventoryUIManager.Instance != null)
                    InventoryUIManager.Instance.ClearDetails();
            }
        }
    }

    private IEnumerator HandleSingleClick()
    {
        yield return new WaitForSeconds(0.2f);

        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.UpdateDetails(currentSlot.item);

        singleClickCoroutine = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1 && InventoryUIManager.Instance != null)
        {
            dropSuccessful = false;
            Sprite icon = Resources.Load<Sprite>(currentSlot.item.iconPath);
            if (icon != null)
            {
                InventoryUIManager.Instance.StartDrag(icon);
                slotIcon.enabled = false;
                if (countText != null) countText.gameObject.SetActive(false);
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (currentSlot == null || currentSlot.slotIndex == -1) return;

        Slot_UI sourceSlot = FindSlotComponent(eventData.pointerDrag);

        if (sourceSlot != null)
        {
            if (sourceSlot.currentSlot == null || sourceSlot.currentSlot.slotIndex == -1) return;
            if (sourceSlot == this) return;

            InventoryManager.Instance.SwapItems(sourceSlot.currentSlot.slotIndex, currentSlot.slotIndex);
            sourceSlot.dropSuccessful = true;
            dropSuccessful = true;
            return;
        }

        EquipmentSlot_UI sourceEquipSlot = FindEquipmentSlotComponent(eventData.pointerDrag);

        if (sourceEquipSlot != null && sourceEquipSlot.currentItem != null)
        {
            bool success = EquipmentManager.Instance.UnequipItem(sourceEquipSlot.currentItem, sourceEquipSlot.equipmentSlotIndex);
            if (success)
            {
                sourceEquipSlot.MarkDropSuccessful();
                dropSuccessful = true;
            }
        }
    }

    private Slot_UI FindSlotComponent(GameObject obj)
    {
        if (obj == null) return null;

        Slot_UI slot = obj.GetComponent<Slot_UI>();
        if (slot != null) return slot;

        slot = obj.GetComponentInParent<Slot_UI>();
        if (slot != null) return slot;

        slot = obj.GetComponentInChildren<Slot_UI>();
        return slot;
    }

    private EquipmentSlot_UI FindEquipmentSlotComponent(GameObject obj)
    {
        if (obj == null) return null;

        EquipmentSlot_UI slot = obj.GetComponent<EquipmentSlot_UI>();
        if (slot != null) return slot;

        slot = obj.GetComponentInParent<EquipmentSlot_UI>();
        if (slot != null) return slot;

        slot = obj.GetComponentInChildren<EquipmentSlot_UI>();
        return slot;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[Slot_UI] OnEndDrag - Slot: {name}, DropSuccessful: {dropSuccessful}, PointerEnter: {eventData.pointerEnter?.name}");

        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag();

        // 드롭 성공하지 않았을 때만 추가 검사
        if (!dropSuccessful)
        {
            // 인벤토리 영역 내에 있는지 확인
            bool isInsideInventory = false;

            if (eventData.pointerEnter != null)
            {
                // 현재 포인터가 위치한 오브젝트가 인벤토리 관련 오브젝트인지 확인
                Transform current = eventData.pointerEnter.transform;
                while (current != null)
                {
                    // 인벤토리 관련 오브젝트 이름들을 체크
                    if (current.name.Contains("Inventory") ||
                        current.name.Contains("Slot") ||
                        current.name.Contains("ScrollView") ||
                        current.name.Contains("Viewport") ||
                        current.name == "FullInventory_Inventory_UI" ||
                        current.name == "Small_Inventory_UI")
                    {
                        isInsideInventory = true;
                        break;
                    }
                    current = current.parent;
                }
            }

            // 인벤토리 밖에 드롭한 경우에만 아이템 드롭
            if (!isInsideInventory && eventData.pointerEnter == null)
            {
                if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1)
                {
                    Debug.Log($"[Slot_UI] Dropping item outside inventory: {currentSlot.item.itemName}");
                    InventoryManager.Instance.DropItem(currentSlot.slotIndex);
                    dropSuccessful = true;
                }
            }
            else
            {
                Debug.Log($"[Slot_UI] Drop cancelled - still inside inventory area");
            }
        }

        // 드롭이 성공하지 않았다면 아이템 아이콘 복구
        if (!dropSuccessful)
            UpdateSlotVisuals();

        dropSuccessful = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1 && InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    private void AttemptEquip()
    {
        if (currentSlot == null || currentSlot.item == null || currentSlot.slotIndex == -1) return;

        RelicData itemToEquip = currentSlot.item;
        string currentItemTypeString = itemToEquip.itemTypeEnum.ToString().ToLower();

        bool isEquippable = false;
        foreach (var type in EquippableTypes)
        {
            if (currentItemTypeString == type)
            {
                isEquippable = true;
                break;
            }
        }

        if (isEquippable)
        {
            EquipmentManager.Instance.EquipItemToFirstAvailableSlot(itemToEquip, currentSlot.slotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (currentSlot == null || currentSlot.item == null || currentSlot.slotIndex == -1) return;

        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(currentSlot.item));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = null;

        if (hideTooltipCoroutine != null) StopCoroutine(hideTooltipCoroutine);
        hideTooltipCoroutine = StartCoroutine(HideTooltipAfterDelay(0.1f));
    }

    private IEnumerator ShowTooltipAfterDelay(RelicData item)
    {
        yield return new WaitForSeconds(TooltipDelay);
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.ShowTooltip(item, transform.position);
        }
    }

    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (InventoryUIManager.Instance != null)
        {
            InventoryUIManager.Instance.HideTooltip();
        }
        hideTooltipCoroutine = null;
    }

    private bool IsMainInventoryView()
    {
        return InventoryManager.Instance.currentFilter != InventoryFilterType.Relic;
    }
}