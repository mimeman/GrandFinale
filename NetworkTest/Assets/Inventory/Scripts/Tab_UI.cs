// Tab_UI.cs 수정
using UnityEngine;
using UnityEngine.UI;

public class Tab_UI : MonoBehaviour
{
    // L1: 인스펙터에서 이 탭이 어떤 종류의 아이템을 필터링할지 설정
    public InventoryFilterType filterType;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnTabClicked);
        }
    }

    private void OnTabClicked()
    {
        // L2: 클릭 시 InventoryManager의 필터 설정 함수 호출
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetFilter(filterType);
        }
    }
}