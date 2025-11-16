using UnityEngine;
using System.Collections.Generic;

public class LootOrbVisuals : MonoBehaviour
{
    [Header("등급별 VFX 프리팹 (자식 오브젝트)")]
    public GameObject vfxCommon;
    public GameObject vfxRare;
    public GameObject vfxEpic;


    // 이 필드를 GenericLootDrop 프리팹의 Inspector에서 VFX_Center 오브젝트와 연결해야 합니다.
    [Header("중심점 오브젝트")]
    public GameObject vfxCenter;

    // 성능을 위해 파티클 시스템 컴포넌트를 미리 캐시합니다.
    private ParticleSystem commonPS;
    private ParticleSystem rarePS;
    private ParticleSystem epicPS;


    private void Awake()
    {
        // 1. Awake에서 ParticleSystem 컴포넌트를 미리 찾아서 캐시합니다.
        if (vfxCommon) commonPS = vfxCommon.GetComponent<ParticleSystem>();
        if (vfxRare) rarePS = vfxRare.GetComponent<ParticleSystem>();
        if (vfxEpic) epicPS = vfxEpic.GetComponent<ParticleSystem>();
    }

    public void Initialize(string grade)
    {
        // 1. 안전을 위해 모든 VFX 오브젝트를 비활성화합니다. 
        if (vfxCommon) vfxCommon.SetActive(false);
        if (vfxRare) vfxRare.SetActive(false);
        if (vfxEpic) vfxEpic.SetActive(false);

        ParticleSystem targetPS = null;
        GameObject targetVFXObject = null;

        // 2. 등급에 맞는 VFX 선택
        switch (grade)
        {
            case "Common":
                if (vfxCommon) { targetVFXObject = vfxCommon; targetPS = commonPS; }
                break;
            case "Rare":
                if (vfxRare) { targetVFXObject = vfxRare; targetPS = rarePS; }
                break;
                break;
            case "Epic":
                if (vfxEpic) { targetVFXObject = vfxEpic; targetPS = epicPS; }
                break;
            default:
                if (vfxCommon) { targetVFXObject = vfxCommon; targetPS = commonPS; }
                break;
        }

        if (targetVFXObject != null)
        {

            // 3. VFX 오브젝트 활성화
            targetVFXObject.SetActive(true);

            if (targetPS != null)
            {
                targetPS.Play();
            }
            else
            {
                Debug.LogError($"[VFX ERROR] '{grade}' 등급 VFX 오브젝트에 ParticleSystem 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError($"[VFX ERROR] '{grade}' 등급에 해당하는 VFX 프리팹이 연결되지 않았습니다.");
        }

    }
}