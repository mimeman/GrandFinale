using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using UnityEngine.EventSystems;

public class InventoryUIManager : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public static InventoryUIManager Instance { get; private set; }

    [Header("UI Movement")]
    // 이 스크립트가 붙은 패널 자체 (RectTransform)
    [SerializeField] private RectTransform rectTransform;
    // 드래그 가능한 영역 (Header_Bar)
    [SerializeField] private RectTransform headerBarRect;
    private Vector2 dragOffset;

    [Header("Drag & Drop")]
    [SerializeField] private Image dragIcon;

    [Header("Inventory Details")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject statsBoxObject;

    [Header("Item Tooltip")] // ★ L26: Tooltip 관련 필드 추가
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipTitleText;
    [SerializeField] private TextMeshProUGUI tooltipDescriptionText;

    [SerializeField] private Vector2 tooltipOffset = new Vector2(20f, -50f); // 마우스 오른쪽 아래에 표시

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            // Destroy(gameObject); 
        }

        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (tooltipPanel) tooltipPanel.SetActive(false);
    }

    void Start()
    {
        // Full UI에 붙어 있을 경우만 Instance를 설정 (Small UI는 Detail Panel이 없으므로)
        // 이 로직은 실제 프로젝트 구성에 따라 달라질 수 있습니다.
        if (titleText != null && Instance == null)
        {
            Instance = this;
        }
    }


    #region UI Movement Functions

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        RectTransform targetRect = headerBarRect != null ? headerBarRect : rectTransform;

        // Header Bar 영역 안에서 드래그를 시작했는지 확인
        if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, eventData.position, eventData.pressEventCamera))
        {
            // 마우스 포인터의 스크린 좌표를 부모의 로컬 좌표로 변환
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent.GetComponent<RectTransform>(),
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            ))
            {
                // 드래그 오프셋 계산 (패널의 현재 위치 - 마우스의 현재 위치)
                dragOffset = rectTransform.anchoredPosition - localPoint;
            }
        }
        else
        {
            // Header Bar 밖에서 드래그 시작 시 이벤트 무시
            //eventData.pointerDrag = null;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Vector2 localPointerPosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPosition
        ))
        {
            // 마우스 현재 위치 + 오프셋 = 새로운 앵커드 포지션
            rectTransform.anchoredPosition = localPointerPosition + dragOffset;
        }
    }

    // InventoryUIManager.cs (ShowTooltip 함수)

    public void ShowTooltip(RelicData item, Vector3 slotScreenPosition)
    {
        if (tooltipPanel == null || tooltipPanel.transform.parent == null) return;

        // 1. 위치 설정 로직 (기존 로직 유지)
        RectTransform parentCanvas = tooltipPanel.transform.parent.GetComponent<RectTransform>();
        Vector2 localPointerPosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas,
            Input.mousePosition,
            null,
            out localPointerPosition))
        {
            RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                tooltipRect.localPosition = localPointerPosition + tooltipOffset;
            }
        }

        // 2. 제목, 등급, 설명 설정
        tooltipTitleText.text = $"<color={GetGradeColor(item.grade)}>{item.itemName}</color> ({item.grade})"; // 등급 추가
        tooltipDescriptionText.text = item.description;

        // 3. 능력치 및 상세 정보 포맷팅 (개선)
        StringBuilder detailsBuilder = new StringBuilder();

        // 아이템 타입 (예: 유물, 기타)
        detailsBuilder.AppendLine($"\n**타입**: {item.itemTypeEnum}");

        // 아이템 스택 정보
        if (item.maxStack > 1)
        {
            detailsBuilder.AppendLine($"**최대 스택**: {item.maxStack}개");
        }

        // 능력치 정보
        if (item.grantedAbility != null)
        {
            AbilityData ability = item.grantedAbility;
            detailsBuilder.AppendLine("\n**--- 부여 능력 ---**");

            // ABIL_004: 최대 체력 증가 (Stat_Add)
            if (ability.abilityLogicID == "Stat_Add")
            {
                detailsBuilder.AppendLine($"+{ability.param_ValueA} {ability.param_Key}");
            }
            // ABIL_005, ABIL_006, ABIL_008: % 증가 (Stat_Percent)
            else if (ability.abilityLogicID.Contains("Stat_Percent"))
            {
                detailsBuilder.AppendLine($"+{ability.param_ValueA}% {ability.param_Key} 증가");
            }
            // ABIL_007: 무한 탄창 (ActiveApply_Self_Buff)
            else if (ability.abilityLogicID == "Apply_Self_Buff" && ability.param_Key == "InfiniteAmmo")
            {
                detailsBuilder.AppendLine($"**능력**: {ability.abilityName}");
                // param_ValueA: 지속 시간, param_ValueB: 쿨타임 (CSV 데이터 구조에 따라 가정)
                detailsBuilder.AppendLine($"지속 시간: {ability.param_ValueA}초, 쿨타임: {ability.param_ValueB}초");
            }
            else
            {
                // 기타 능력 (기능 없음)
                detailsBuilder.AppendLine($"**능력**: {ability.abilityName}");
            }
        }

        // 툴팁 텍스트에 최종 상세 정보 추가 (새로운 TMP 필드가 있다고 가정)
        // 현재 InventoryUIManager.cs에는 tooltipDescriptionText만 있으므로, 여기에 모두 합칩니다.
        tooltipDescriptionText.text += detailsBuilder.ToString();

        tooltipPanel.SetActive(true);
    }
    // L130: HideTooltip 함수 추가
    public void HideTooltip()
    {
        if (tooltipPanel)
        {
            tooltipPanel.SetActive(false);
        }
    }

    #endregion


    #region Drag & Drop Functions

    public void StartDrag(Sprite iconSprite)
    {
        dragIcon.sprite = iconSprite;
        dragIcon.gameObject.SetActive(true);
    }

    public void UpdateDragIcon(Vector2 position)
    {
        dragIcon.rectTransform.position = position;
    }

    public void EndDrag()
    {
        dragIcon.gameObject.SetActive(false);
        // 드래그가 끝날 때 상세 정보 창을 비웁니다.
        ClearDetails();
    }

    #endregion

    #region Item Details Functions

    public void UpdateDetails(RelicData item)
    {
        if (item == null)
        {
            ClearDetails();
            return;
        }

        titleText.text = $"<color={GetGradeColor(item.grade)}>{item.itemName}</color>";
        descriptionText.text = item.description;

        if (item.grantedAbility != null)
        {
            statsBoxObject.SetActive(true);
            StringBuilder statsBuilder = new StringBuilder();
            AbilityData ability = item.grantedAbility;

            if (ability.abilityLogicID == "Stat_Add")
            {
                statsBuilder.AppendLine($"{ability.param_Key} : +{ability.param_ValueA}");
            }
            else if (ability.abilityLogicID == "Projectile")
            {
                statsBuilder.AppendLine($"능력 : {ability.abilityName}");
                statsBuilder.AppendLine($"쿨타임 : {ability.param_ValueA}초");
            }
            else
            {
                statsBuilder.AppendLine($"능력 : {ability.abilityName}");
            }

            statsText.text = statsBuilder.ToString();
        }
        else
        {
            statsBoxObject.SetActive(false);
        }
    }

    private string GetGradeColor(string grade)
    {
        return grade.ToLower() switch
        {
            "common" => "#FFFFFF",
            "rare" => "#00CCFF",
            "epic" => "#9900FF",
            _ => "#FFFFFF",
        };
    }

    public void ClearDetails()
    {
        if (titleText != null) titleText.text = "인벤토리";
        if (descriptionText != null) descriptionText.text = "";
        if (statsBoxObject != null) statsBoxObject.SetActive(false);
    }

    #endregion
}