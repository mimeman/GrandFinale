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

    private ScrollRect parentScrollRect;
    private bool isScrolling = false;

    private static readonly string[] EquippableTypes =
    {
        ItemType.Weapon.ToString().ToLower(),
        ItemType.Artifact.ToString().ToLower(),
        ItemType.Accessory.ToString().ToLower(),
        ItemType.Equipment.ToString().ToLower()
    };

    void Start()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    public void SetBoundItem(InventorySlot newSlot)
    {
        currentSlot = newSlot;
        UpdateSlotVisuals();
    }

    void UpdateSlotVisuals()
    {
        if (slotIcon == null) return;

        if (HasValidItem())
        {
            DisplayItemIcon();
            DisplayItemCount();
        }
        else
        {
            HideSlot();
        }
    }

    private bool HasValidItem()
    {
        return currentSlot != null && currentSlot.item != null && currentSlot.slotIndex != -1;
    }

    private void DisplayItemIcon()
    {
        Sprite icon = Resources.Load<Sprite>(currentSlot.item.iconPath);

        if (icon != null)
        {
            slotIcon.sprite = icon;
            slotIcon.enabled = true;
        }
        else
        {
            slotIcon.enabled = false;
        }
    }

    private void DisplayItemCount()
    {
        if (countText != null)
        {
            countText.text = currentSlot.quantity.ToString();
            countText.gameObject.SetActive(true);
            countText.fontSize = 10;
        }
    }

    private void HideSlot()
    {
        slotIcon.sprite = null;
        slotIcon.enabled = false;

        if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!HasValidItem()) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            AttemptEquip();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick(eventData);
        }
    }

    private void HandleLeftClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 1)
        {
            HandleSingleClickStart();
        }
        else if (eventData.clickCount == 2)
        {
            HandleDoubleClick();
        }
    }

    private void HandleSingleClickStart()
    {
        if (singleClickCoroutine != null)
            StopCoroutine(singleClickCoroutine);

        if (IsMainInventoryView())
            singleClickCoroutine = StartCoroutine(HandleSingleClick());
    }

    private void HandleDoubleClick()
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

    private IEnumerator HandleSingleClick()
    {
        yield return new WaitForSeconds(0.2f);

        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.UpdateDetails(currentSlot.item);

        singleClickCoroutine = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentScrollRect == null) return;

        if (!HasValidItem())
        {
            // 빈 슬롯: 무조건 스크롤
            isScrolling = true;
            parentScrollRect.OnBeginDrag(eventData);
        }
        else
        {
            // 아이템 있는 슬롯: 무조건 아이템 드래그
            isScrolling = false;

            if (InventoryUIManager.Instance == null) return;

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
            HandleInventorySlotDrop(sourceSlot);
            return;
        }

        EquipmentSlot_UI sourceEquipSlot = FindEquipmentSlotComponent(eventData.pointerDrag);

        if (sourceEquipSlot != null && sourceEquipSlot.currentItem != null)
        {
            HandleEquipmentSlotDrop(sourceEquipSlot);
        }
    }

    private void HandleInventorySlotDrop(Slot_UI sourceSlot)
    {
        if (sourceSlot.currentSlot == null || sourceSlot.currentSlot.slotIndex == -1) return;
        if (sourceSlot == this) return;

        InventoryManager.Instance.SwapItems(sourceSlot.currentSlot.slotIndex, currentSlot.slotIndex);
        sourceSlot.dropSuccessful = true;
        dropSuccessful = true;
    }

    private void HandleEquipmentSlotDrop(EquipmentSlot_UI sourceEquipSlot)
    {
        bool success = EquipmentManager.Instance.UnequipItem(sourceEquipSlot.currentItem, sourceEquipSlot.equipmentSlotIndex);
        if (success)
        {
            sourceEquipSlot.MarkDropSuccessful();
            dropSuccessful = true;
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
        if (isScrolling)
        {
            if (parentScrollRect != null)
            {
                parentScrollRect.OnEndDrag(eventData);
            }
        }
        else
        {
            // 아이템 드래그가 끝났으므로, 슬롯 상태와 관계없이 마우스 아이콘은 무조건 숨김
            if (InventoryUIManager.Instance != null)
                InventoryUIManager.Instance.EndDrag();

            if (HasValidItem())
            {
                if (!dropSuccessful)
                {
                    if (ShouldDropItem(eventData))
                    {
                        DropItemOutsideInventory();
                    }
                    else
                    {
                        UpdateSlotVisuals();
                    }
                }
            }
            else if (!dropSuccessful)
            {
                UpdateSlotVisuals(); // 슬롯이 비어있어도 비주얼 업데이트
            }
        }

        dropSuccessful = false;
        isScrolling = false;
    }

    private bool ShouldDropItem(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null)
        {
            return !IsInsideInventoryArea(eventData);
        }
        return false;
    }

    private bool IsInsideInventoryArea(PointerEventData eventData)
    {
        if (eventData.pointerEnter == null) return false;

        Transform current = eventData.pointerEnter.transform;
        while (current != null)
        {
            if (IsInventoryRelatedObject(current.name))
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

    private bool IsInventoryRelatedObject(string objectName)
    {
        return objectName.Contains("Inventory") ||
               objectName.Contains("Slot") ||
               objectName.Contains("ScrollView") ||
               objectName.Contains("Viewport") ||
               objectName == "FullInventory_Inventory_UI" ||
               objectName == "Small_Inventory_UI";
    }

    private void DropItemOutsideInventory()
    {
        if (HasValidItem())
        {
            InventoryManager.Instance.DropItem(currentSlot.slotIndex);
            dropSuccessful = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isScrolling)
        {
            if (parentScrollRect != null)
            {
                parentScrollRect.OnDrag(eventData);
            }
        }
        else
        {
            // 이 블록은 (isScrolling == false) 일 때만 실행됨 (아이템 드래그 중)
            if (HasValidItem() && InventoryUIManager.Instance != null)
            {
                InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
            }
        }
    }

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    private void AttemptEquip()
    {
        if (!HasValidItem()) return;

        RelicData itemToEquip = currentSlot.item;
        string currentItemTypeString = itemToEquip.itemTypeEnum.ToString().ToLower();

        if (IsEquippableType(currentItemTypeString))
        {
            EquipmentManager.Instance.EquipItemToFirstAvailableSlot(itemToEquip, currentSlot.slotIndex);
        }
    }

    private bool IsEquippableType(string itemTypeString)
    {
        foreach (var type in EquippableTypes)
        {
            if (itemTypeString == type)
            {
                return true;
            }
        }
        return false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (!HasValidItem()) return;

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