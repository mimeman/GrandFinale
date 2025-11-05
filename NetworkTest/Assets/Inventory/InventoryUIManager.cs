using UnityEngine;
using UnityEngine.UI; 
using TMPro;         
using System.Text;   

public class InventoryUIManager : MonoBehaviour
{
    public static InventoryUIManager Instance;

    [Header("Drag & Drop")]
    [SerializeField] private Image dragIcon; // 마우스를 따라다닐 '유령 아이KON'

    [Header("Inventory Details")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject statsBoxObject; // 스탯 박스 (Stats_Text_Image)
        
    void Awake()
    {
        // 1-1. 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 중복 생성 방지
            Destroy(gameObject);
        }
    }



    #region Drag & Drop Functions

    public void StartDrag(Sprite iconSprite)
    {
        dragIcon.sprite = iconSprite;
        dragIcon.gameObject.SetActive(true);
    }

    public void UpdateDragIcon(Vector2 mousePosition)
    {
        dragIcon.transform.position = mousePosition;
    }

    public void EndDrag()
    {
        dragIcon.sprite = null;
        dragIcon.gameObject.SetActive(false);
    }

    #endregion

    #region Details Panel Functions

    /// <summary>
    /// (Slot_UI가 호출) RelicData 정보를 받아 Details 패널에 표시합니다.
    /// </summary>
    public void UpdateDetails(RelicData item)
    {
        if (item == null)
        {
            ClearDetails();
            return;
        }

        titleText.text = item.itemName;
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

    /// <summary>
    /// 상세정보 창을 비웁니다.
    /// </summary>
    private void ClearDetails()
    {
        titleText.text = "인벤토리";
        descriptionText.text = "아이템을 선택하세요.";
        statsBoxObject.SetActive(false);
    }

    #endregion
}