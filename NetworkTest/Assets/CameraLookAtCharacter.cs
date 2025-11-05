// CameraLookAtCharacter.cs (간단 버전)
using UnityEngine;
public class CameraLookAtCharacter : MonoBehaviour
{
    public Transform targetCharacter;
    // 목표 위치 오프셋을 캐릭터 정면으로 설정합니다.
    public Vector3 offset = new Vector3(0, 1.0f, -2.0f);

    void LateUpdate()
    {
        if (targetCharacter != null)
        {
            // 위치만 캐릭터를 따라가고, 회전은 건드리지 않거나 고정합니다.
            transform.position = targetCharacter.position + offset;
            // [선택 사항] 회전을 강제 고정: transform.rotation = Quaternion.Euler(0, 180, 0); 
        }
    }
}