using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class EquipmentSlot_UI : MonoBehaviour, IDropHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler,
    IPointerEnterHandler, IPointerExitHandler 
{
    [Header("필터 설정")]
    public EquipmentSlot requiredSlotType = EquipmentSlot.None;
    public ItemType requiredItemType = ItemType.Etc;

    [Header("UI (선택 사항)")]
    public Image slotIcon;
    [Tooltip("아이템 이름을 표시할 텍스트 (선택 사항)")]
    public TextMeshProUGUI slotNameText;

    public int equipmentSlotIndex;

    public RelicData currentItem { get; private set; }
    public bool dropSuccessful = false;

    private bool isIgnoringClick = false;
    private Coroutine singleClickCoroutine;

    private Coroutine hideTooltipCoroutine;
    private Coroutine tooltipCoroutine;
    private const float TooltipDelay = 0.5f;

    void Start()
    {
        EquipmentManager.OnEquipmentChanged += UpdateSlotVisuals;
        UpdateSlotVisuals();
    }

    void OnDestroy()
    {
        EquipmentManager.OnEquipmentChanged -= UpdateSlotVisuals;
    }

    void UpdateSlotVisuals()
    {
        if (EquipmentManager.Instance == null) return;

        currentItem = EquipmentManager.Instance.equipmentSlots[equipmentSlotIndex];

        if (currentItem != null && !string.IsNullOrEmpty(currentItem.iconPath))
        {
            Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);
            if (icon != null)
            {
                slotIcon.sprite = icon;
                slotIcon.enabled = true;
            }
            else
            {
                slotIcon.enabled = false;
            }

            if (slotNameText != null)
            {
                slotNameText.text = currentItem.itemName;
                slotNameText.gameObject.SetActive(true);
            }
        }
        else
        {
            slotIcon.sprite = null;
            slotIcon.enabled = false;

            if (slotNameText != null)
            {
                slotNameText.text = "";
                slotNameText.gameObject.SetActive(false);
            }
        }
    }


    public void OnDrop(PointerEventData eventData)
    {
        Slot_UI sourceSlot = eventData.pointerDrag.GetComponent<Slot_UI>();

        if (sourceSlot == null || sourceSlot.currentSlot == null || sourceSlot.currentSlot.item == null || sourceSlot.currentSlot.slotIndex == -1)
        {
            return;
        }

        RelicData itemToEquip = sourceSlot.currentSlot.item;

        if (CanEquipItem(itemToEquip))
        {
            bool success = EquipmentManager.Instance.EquipItem(itemToEquip, sourceSlot.currentSlot.slotIndex, this.equipmentSlotIndex);

            if (success)
            {
                sourceSlot.dropSuccessful = true;
                StartCoroutine(IgnoreNextClick());
            }
        }
        else
        {
            Debug.Log($"[장착 실패] 이 슬롯은 ({requiredSlotType} / {requiredItemType}) 타입만 받습니다. (아이템 정보: {itemToEquip.equipmentSlot} / {itemToEquip.itemTypeEnum})");
        }
    }

    private IEnumerator IgnoreNextClick()
    {
        isIgnoringClick = true;
        yield return null;
        isIgnoringClick = false;
    }

    public bool CanEquipItem(RelicData item)
    {
        if (item == null) return false;

        if (requiredSlotType != EquipmentSlot.None)
        {
            return item.equipmentSlot == requiredSlotType;
        }

        if (requiredItemType != ItemType.Etc)
        {
            return item.itemTypeEnum == requiredItemType;
        }

        Debug.LogWarning($"EquipmentSlot_UI (인덱스 {equipmentSlotIndex})에 필터가 설정되지 않았습니다.");
        return false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            dropSuccessful = false;
            Sprite icon = Resources.Load<Sprite>(currentItem.iconPath);
            if (icon != null)
            {
                InventoryUIManager.Instance.StartDrag(icon);
                slotIcon.enabled = false;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            InventoryUIManager.Instance.UpdateDragIcon(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.EndDrag();

        if (currentItem == null) return;

        if (!dropSuccessful && eventData.pointerEnter != null)
        {
            slotIcon.enabled = true;
        }

        dropSuccessful = false;
    }

    public void MarkDropSuccessful()
    {
        dropSuccessful = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isIgnoringClick)
        {
            isIgnoringClick = false;
            return;
        }

        if (currentItem == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            UnequipItemAttempt();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (eventData.clickCount == 1)
            {
                if (singleClickCoroutine != null)
                {
                    StopCoroutine(singleClickCoroutine);
                }
                singleClickCoroutine = StartCoroutine(HandleSingleClick());
            }
            else if (eventData.clickCount == 2)
            {
                if (singleClickCoroutine != null)
                {
                    StopCoroutine(singleClickCoroutine);
                    singleClickCoroutine = null;
                }

                UnequipItemAttempt();
            }
        }
    }

    private IEnumerator HandleSingleClick()
    {
        yield return new WaitForSeconds(0.2f);

        if (InventoryUIManager.Instance != null)
            InventoryUIManager.Instance.UpdateDetails(currentItem);

        singleClickCoroutine = null;
    }

    private void UnequipItemAttempt()
    {
        if (currentItem == null) return;

        bool success = EquipmentManager.Instance.UnequipItem(currentItem, this.equipmentSlotIndex);

        if (!success)
        {
            Debug.Log("인벤토리가 꽉 차서 장비를 해제할 수 없습니다.");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;
        }

        if (currentItem == null) return;

        if (tooltipCoroutine != null) StopCoroutine(tooltipCoroutine);
        tooltipCoroutine = StartCoroutine(ShowTooltipAfterDelay(currentItem));
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
}