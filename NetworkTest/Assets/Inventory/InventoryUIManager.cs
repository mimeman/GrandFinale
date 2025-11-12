using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using UnityEngine.EventSystems;
using System.Collections;

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

    [Header("Item Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private RectTransform tooltipRect;

    [Header("Tooltip Content References")]
    [SerializeField] private TextMeshProUGUI tooltipTitleText; // 좌상단 아이템 이름
    [SerializeField] private TextMeshProUGUI tooltipItemKindText; // 좌상단 아이템 종류
    [SerializeField] private Image tooltipItemImage; // 우상단 아이템 이미지
    [SerializeField] private Image tooltipGradeImage; // 우상단 아이템 등급 사진 (선택 사항: 등급 텍스트로 대체 가능)
    [SerializeField] private TextMeshProUGUI tooltipGradeText; // 우상단 아이템 등급 텍스트
    [SerializeField] private TextMeshProUGUI tooltipDescriptionText; // 하단부 아이템 설명 및 스탯

    [SerializeField] private Vector2 tooltipOffset = new Vector2(20f, -50f); // 마우스 오른쪽 아래에 표시

    private Coroutine fadeCoroutine;
    private CanvasGroup tooltipCanvasGroup;
    private const float FadeDuration = 0.2f;
    private float currentTargetAlpha = 0f;

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

        if (tooltipPanel)
        {
            tooltipCanvasGroup = tooltipPanel.GetComponent<CanvasGroup>();

            if (tooltipCanvasGroup == null)
            {
                tooltipCanvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }
            tooltipCanvasGroup.blocksRaycasts = false;

            tooltipCanvasGroup.alpha = 0f; // 시작 시 투명하게
            tooltipPanel.SetActive(false); // 시작 시 비활성화
        }
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

    // InventoryUIManager.cs

    public void ShowTooltip(RelicData item, Vector3 slotScreenPosition)
    {
        if (tooltipPanel == null) return;

        // **수정 2: 목표 알파가 이미 1f인 경우 (이미 켜짐/켜는 중) 코루틴 재시작 방지**
        if (currentTargetAlpha == 1f && tooltipCanvasGroup.alpha > 0f)
        {
            // 툴팁의 내용과 위치는 매번 업데이트해야 하므로 아래 로직을 수행하도록 리턴하지 않습니다.
            // 다만, 이미 페이드 인 중이므로 코루틴은 재시작하지 않습니다.
        }
        else
        {
            // 페이드 아웃 중이거나 꺼진 상태에서 다시 켜야 할 경우
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            // 툴팁 패널 활성화
            tooltipPanel.SetActive(true);

            // 목표 알파값 설정 및 새 코루틴 시작
            currentTargetAlpha = 1f;
            fadeCoroutine = StartCoroutine(FadeTooltip(1f));
        }


        // 2. 내용 설정 (아이콘, 텍스트) - 위치 업데이트를 위해 페이드 상태와 상관없이 실행
        UpdateTooltipContent(item);

        // 3. 위치 설정 (기존 로직 유지)
        RectTransform parentCanvas = tooltipPanel.transform.parent.GetComponent<RectTransform>();
        Vector2 localPointerPosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas,
            Input.mousePosition,
            null,
            out localPointerPosition))
        {
            if (tooltipRect != null)
            {
                // 오프셋 적용 및 위치 설정
                tooltipRect.localPosition = localPointerPosition + tooltipOffset;
            }
        }
    }
    public void HideTooltip()
    {
        if (tooltipPanel == null) return;

        // 목표가 이미 0f이면 재시작 안 함
        if (currentTargetAlpha == 0f) return;

        // 현재 페이드 코루틴 중단
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        // 페이드 아웃 시작
        currentTargetAlpha = 0f; // 목표값 설정
        fadeCoroutine = StartCoroutine(FadeTooltip(0f));
    }

    private IEnumerator FadeTooltip(float targetAlpha)
    {
        float startAlpha = tooltipCanvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < FadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / FadeDuration);
            tooltipCanvasGroup.alpha = currentAlpha;
            yield return null;
        }

        tooltipCanvasGroup.alpha = targetAlpha; // 정확한 값으로 설정
        currentTargetAlpha = targetAlpha;

        // 완전히 투명해졌다면 오브젝트 비활성화
        if (targetAlpha == 0f)
        {
            tooltipPanel.SetActive(false);
        }
        fadeCoroutine = null;
    }

    private void UpdateTooltipContent(RelicData item)
    {
        // 1. 아이템 이름 (등급 색상 적용)
        tooltipTitleText.text = $"<color={GetGradeColor(item.grade)}>{item.itemName}</color>";

        // 2. 아이템 종류 텍스트 (예: Weapon, Artifact)
        if (tooltipItemKindText != null)
        {
            tooltipItemKindText.text = item.itemTypeEnum.ToString();
        }

        // 3. 아이템 이미지
        if (tooltipItemImage != null)
        {
            Sprite icon = Resources.Load<Sprite>(item.iconPath);
            tooltipItemImage.sprite = icon;
            tooltipItemImage.enabled = (icon != null);
        }

        // 4. 아이템 등급 텍스트
        if (tooltipGradeText != null)
        {
            tooltipGradeText.text = item.grade;
            tooltipGradeText.color = ColorUtility.TryParseHtmlString(GetGradeColor(item.grade), out Color color) ? color : Color.white;
        }

        // 5. 등급 사진 (선택 사항, 등급 텍스트로 대체 가능)
        // if (tooltipGradeImage != null) { ... } 

        // 6. 설명 및 상세 정보 (하단부)
        StringBuilder detailsBuilder = new StringBuilder();
        //detailsBuilder.AppendLine($"타입: { item.itemTypeEnum}");
        //detailsBuilder.AppendLine($"등급: { item.grade}");

        // 아이템 설명
        detailsBuilder.AppendLine($"\n{item.description}");

        // 능력치 정보 추가 (기존 로직 사용)
        if (item.grantedAbility != null)
        {
            // ... (기존 ShowTooltip의 능력치 포맷팅 로직 그대로 사용)
            AbilityData ability = item.grantedAbility;
            detailsBuilder.AppendLine("\n--- 부여 능력 ---");

            if (ability.abilityLogicID == "Stat_Add")
            {
                detailsBuilder.AppendLine($"+{ability.param_ValueA} {ability.param_Key}");
            }
            else // 기타 능력
            {
                detailsBuilder.AppendLine($"능력: {ability.abilityName}");
            }
        }

        tooltipDescriptionText.text = detailsBuilder.ToString();
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