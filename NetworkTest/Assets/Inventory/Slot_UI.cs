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
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag();

        if (eventData.pointerEnter == null && !dropSuccessful)
        {
            if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1)
            {
                InventoryManager.Instance.DropItem(currentSlot.slotIndex);
                dropSuccessful = true;
            }
        }

        if (!dropSuccessful)
            UpdateSlotVisuals();

        if (currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1 && IsMainInventoryView())
        {
            if (InventoryUIManager.Instance != null)
                InventoryUIManager.Instance.UpdateDetails(currentSlot.item);
        }

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