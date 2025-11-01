using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamageText : MonoBehaviour
{
    public TMP_Text text;   
    public float floatDistance = 1f;
    public float duration = 1f;

    void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();
    }

    public void ShowDamage(int damage)
    {
        text.text = damage.ToString(); // 텍스트 숫자 설정

        Vector3 targetPos = transform.position + Vector3.up * floatDistance; // 현재 위치에서 위로 이동할 목표 위치 계산
        transform.DOMove(targetPos, duration).SetEase(Ease.OutCubic); // 텍스트를 위로 이동시키는 애니메이션 (DOTween)
        text.DOFade(0, duration).SetEase(Ease.InQuad); // 텍스트의 알파값을 점점 0으로 줄이는 페이드 아웃 효과

        Destroy(gameObject, duration + 0.1f);
    }

    void Update()
    {
        if (Camera.main != null)
        {
            // 항상 카메라 정면을 바라보게 회전
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180f, 0); // 텍스트가 뒤집히는 경우 대응
        }
    }
}

/*
=== 사용법 ===
- 이 스크립트를 포함한 프리팹을 만들고, DamageText 오브젝트를 Instantiate하여 사용
- ShowDamage(int damage) 메서드를 호출하여 데미지를 전달
- 예: Instantiate(prefab, 위치, 회전).GetComponent<DamageText>().ShowDamage(100);

=== 특징 ===
- DOTween을 이용한 부드러운 이동 및 페이드 아웃 애니메이션
- 텍스트가 항상 카메라를 바라보도록 처리 (월드 공간 캔버스 기반)
- 일정 시간 후 자동 파괴

=== 개선 아이디어 ===
- 치명타일 경우 색상 변경 또는 크기 증가
- 텍스트에 랜덤한 회전, 흔들림 효과 추가
- 데미지 종류별로 색상 구분 (예: 화염, 독 등)
*/