using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Ability : MonoBehaviour
{
    [Header("Ability 1")]
    public Image abilityImage1;
    public Text abilityText1;
    public KeyCode ability1Key;
    public float ability1Cooldown = 5;

    public Canvas ability1Canvas;
    public Image ability1SkillShot;

    [Header("Ability 2")]
    public Image abilityImage2;
    public Text abilityText2;
    public KeyCode ability2Key;
    public float ability2Cooldown = 7;

    public Canvas ability2Canvas;
    public Image ability2RangeIndicator;
    public float maxAbility2Distance = 7;

    [Header("Ability 3")]
    public Image abilityImage3;
    public Text abilityText3;
    public KeyCode ability3Key;
    public float ability3Cooldown = 10;

    public Canvas ability3Canvas;
    public Image ability3Cone;

    bool isAbility1Cooldown = false;
    bool isAbility2Cooldown = false;
    bool isAbility3Cooldown = false;

    float currentAbility1Cooldown;
    float currentAbility2Cooldown;
    float currentAbility3Cooldown;

    private bool isSkill1FixedMode = false; // Skill1이 고정 모드인지 추적
    private bool isESkillLockOnMode = false;    // E스킬이 락온 모드인지
    private Vector3 eSkillFixedPosition;        // 락온 시 고정 위치

    Vector3 position;
    RaycastHit hit;
    Ray ray;

    private void Start()
    {
        abilityImage1.fillAmount = 0;
        abilityImage2.fillAmount = 0;
        abilityImage3.fillAmount = 0;

        abilityText1.text = "";
        abilityText2.text = "";
        abilityText3.text = "";

        ability1SkillShot.enabled = false;
        ability2RangeIndicator.enabled = false;
        ability3Cone.enabled = false;

        ability1Canvas.enabled = false;
        ability2Canvas.enabled = false;
        ability3Canvas.enabled = false;

    }

    private void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        Ability1Input();
        Ability2Input();
        Ability3Input();

        AbilityCooldown(ref currentAbility1Cooldown, ability1Cooldown, ref isAbility1Cooldown, abilityImage1, abilityText1);
        AbilityCooldown(ref currentAbility2Cooldown, ability2Cooldown, ref isAbility2Cooldown, abilityImage2, abilityText2);
        AbilityCooldown(ref currentAbility3Cooldown, ability3Cooldown, ref isAbility3Cooldown, abilityImage3, abilityText3);

        // Canvas 업데이트 - Skill1은 고정모드가 아닐 때만
        if (!isSkill1FixedMode)
        {
            Ability1Canvas();
        }

        // E스킬이 마우스 추적 모드일 때만 업데이트
        if (!isESkillLockOnMode)
        {
            Ability2Canvas();
        }

        Ability3Canvas();

    }

    void Ability1Canvas()
    {
        if (!ability1SkillShot.enabled) return;

        // 마우스 위치로부터 레이 쏘기
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // 목표 방향(플레이어→마우스 충돌 지점)
            Vector3 dir = hit.point - transform.position;

            // 평면화: Y 성분 제거 후 정규화
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            // LookRotation: dir을 바라보는 회전 생성
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            // Yaw만 적용(X/Z=0)
            var e = rot.eulerAngles;
            rot = Quaternion.Euler(0f, e.y, 0f);

            // 불필요한 Lerp( t=0 ) 제거 → 바로 대입
            ability1Canvas.transform.rotation = rot;
        }
    }

    void Ability2Canvas()
    {
        int layerMask = ~LayerMask.GetMask("Player"); // Player 레이어만 제외

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            // 플레이어→히트 포인트 방향(평면)
            Vector3 start = transform.position;
            float fixedY = start.y;

            Vector3 dir = (hit.point - start);
            dir.y = 0f;                                // 평면화
            float dist = dir.magnitude;

            if (dist > 0.0001f) dir /= dist;

            // 최대 사거리 제한
            float clamped = Mathf.Min(dist, maxAbility2Distance);

            // 최종 위치(Y 고정)
            Vector3 newPos = start + dir * clamped;
            newPos.y = fixedY;

            ability2Canvas.transform.position = newPos;
        }
    }

    void Ability3Canvas()
    {
        if (!ability3Cone.enabled) return;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Vector3 dir = hit.point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            dir.Normalize();

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            var e = rot.eulerAngles;
            rot = Quaternion.Euler(0f, e.y, 0f);

            // 기존 코드: ability3Canvas 회전을 구하면서 ability1Canvas.rotation을 Lerp의 두 번째 인자로 사용
            // 쓸모없는 Lerp 제거, 자기 자신에 직접 대입
            ability3Canvas.transform.rotation = rot;
        }
    }

    void Ability1Input() 
    {
        if(Input.GetKeyDown(ability1Key) && !isAbility1Cooldown)
        {
            ability1Canvas.enabled = true;
            ability1SkillShot.enabled = true;

            ability2Canvas.enabled = false;
            ability2RangeIndicator.enabled = false;
            ability3Canvas.enabled = false;
            ability3Cone.enabled = false;

            Cursor.visible = true;
        }

        if (ability1SkillShot.enabled && Input.GetMouseButtonDown(0))
        {
            isAbility1Cooldown = true;
            currentAbility1Cooldown = ability1Cooldown;

            ability1Canvas.enabled = false;
            ability1SkillShot.enabled = false;

        }
    }

    void Ability2Input()
    {
        if (Input.GetKeyDown(ability2Key) && !isAbility2Cooldown)
        { 
            ability2Canvas.enabled = true;
            ability2RangeIndicator.enabled = true;

            ability1Canvas.enabled = false;
            ability1SkillShot.enabled = false;
            ability3Canvas.enabled = false;
            ability3Cone.enabled = false;

            Cursor.visible = false;
        }

        if (ability2Canvas.enabled && Input.GetMouseButtonDown(0))
        {
            isAbility2Cooldown = true;
            currentAbility2Cooldown = ability2Cooldown;

            ability2Canvas.enabled = false;
            ability2RangeIndicator.enabled = false;

            Cursor.visible = true;

        }
    }

    void Ability3Input()
    {
        if (Input.GetKeyDown(ability3Key) && !isAbility3Cooldown)
        {
            ability3Canvas.enabled = true;
            ability3Cone.enabled = true;

            ability1Canvas.enabled = false;
            ability1SkillShot.enabled = false;
            ability2Canvas.enabled = false;
            ability2RangeIndicator.enabled = false;

            Cursor.visible = true;
        }

        if (ability3Cone.enabled && Input.GetMouseButtonDown(0))
        {
            isAbility3Cooldown = true;
            currentAbility3Cooldown = ability3Cooldown;

            ability3Canvas.enabled = false;
            ability3Cone.enabled = false;
        }
    }

    void AbilityCooldown(ref float currentCooldown, float maxCooldown, ref bool isCooldown, Image skillImage, Text skillText)
    {
        if (isCooldown)
        {
            currentCooldown -= Time.deltaTime;

            if(currentCooldown <= 0f)
            {
                isCooldown = false;
                currentCooldown = 0f;

                if(skillImage != null)
                {
                    skillImage.fillAmount = 0f;
                }

                if(skillText != null)
                {
                    skillText.text = "";
                }
            }
            else
            {
                if(skillImage != null)
                {
                    skillImage.fillAmount = currentCooldown / maxCooldown;
                }

                if(skillText != null)
                {
                    skillText.text = Mathf.Ceil(currentCooldown).ToString();
                }
            }
        }
    }

    /// <summary>
    /// Skill1 전용 인디케이터 표시 - PlayerSkill에서 호출
    /// 고정된 방향으로 화살표 표시 후 자동 삭제
    public void ShowSkill1Indicator(Vector3 direction, float duration)
    {
        Debug.Log($"[Ability] Skill1 인디케이터 표시: 방향={direction}, 지속시간={duration}초");

        // 고정 모드 활성화
        isSkill1FixedMode = true;

        // 다른 스킬 인디케이터들 숨기기
        HideAllIndicators();

        // Skill1 인디케이터 활성화
        ability1Canvas.enabled = true;
        ability1SkillShot.enabled = true;

        // 고정된 방향으로 회전 설정
        SetSkill1Direction(direction);

        // 지정된 시간 후 자동 숨김
        StartCoroutine(HideSkill1IndicatorAfterDelay(duration));
    }

    /// <summary>
    /// Skill1 인디케이터 방향 설정 - 고정 방향 (마우스 추적 안함)
    /// </summary>
    /// <param name="direction">설정할 방향</param>
    private void SetSkill1Direction(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            // Y축 회전만 적용 (XZ 평면에서의 방향)
            direction.y = 0;
            direction.Normalize();

            // Canvas를 해당 방향으로 회전
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            targetRotation.eulerAngles = new Vector3(0, targetRotation.eulerAngles.y, 0);

            ability1Canvas.transform.rotation = targetRotation;

            Debug.Log($"[Ability] Skill1 방향 설정: {targetRotation.eulerAngles.y}도");
        }
    }

    /// <summary>
    /// 지정된 시간 후 Skill1 인디케이터 숨김
    /// </summary>
    /// <param name="delay">대기 시간</param>
    private IEnumerator HideSkill1IndicatorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Skill1 인디케이터 숨김
        if (ability1Canvas != null)
            ability1Canvas.enabled = false;

        if (ability1SkillShot != null)
            ability1SkillShot.enabled = false;

        // 고정 모드 해제
        isSkill1FixedMode = false;

        Debug.Log("[Ability] Skill1 인디케이터 자동 숨김 및 고정모드 해제");
    }

    /// <summary>
    /// 모든 인디케이터 숨기기 - 충돌 방지
    /// </summary>
    private void HideAllIndicators()
    {
        // Ability 1
        if (ability1Canvas != null)
            ability1Canvas.enabled = false;
        if (ability1SkillShot != null)
            ability1SkillShot.enabled = false;

        // Ability 2
        if (ability2Canvas != null)
            ability2Canvas.enabled = false;
        if (ability2RangeIndicator != null)
            ability2RangeIndicator.enabled = false;

        // Ability 3
        if (ability3Canvas != null)
            ability3Canvas.enabled = false;
        if (ability3Cone != null)
            ability3Cone.enabled = false;

        // 모드 초기화
        isSkill1FixedMode = false;
        isESkillLockOnMode = false;
    }

    /// <summary>
    /// E스킬 인디케이터 표시 - PlayerSkill에서 호출
    /// 장판형 범위 인디케이터 (마우스 추적 또는 락온 위치 고정)
    /// </summary>
    /// <param name="targetPosition">스킬 타겟 위치</param>
    public void ShowESkillIndicator(Vector3 targetPosition)
    {
        Debug.Log($"[Ability] E스킬 인디케이터 표시: 위치={targetPosition}");

        // 다른 스킬 인디케이터들 숨기기
        HideAllIndicators();

        // E스킬(Ability2) 인디케이터 활성화
        ability2Canvas.enabled = true;
        ability2RangeIndicator.enabled = true;

        // 락온 모드인지 확인
        PlayerLockOn lockOn = FindFirstObjectByType<PlayerLockOn>();
        if (lockOn != null && lockOn.isLockOn && lockOn.currentTarget != null)
        {
            // 락온 모드: 고정 위치
            isESkillLockOnMode = true;
            eSkillFixedPosition = targetPosition;
            SetESkillPosition(targetPosition);
        }
        else
        {
            // 마우스 추적 모드
            isESkillLockOnMode = false;
        }

        Debug.Log($"[Ability] E스킬 모드: {(isESkillLockOnMode ? "락온 고정" : "마우스 추적")}");
    }

    /// <summary>
    /// E스킬 인디케이터 숨기기
    /// </summary>
    public void HideESkillIndicator()
    {
        Debug.Log("[Ability] E스킬 인디케이터 숨김");

        if (ability2Canvas != null)
            ability2Canvas.enabled = false;

        if (ability2RangeIndicator != null)
            ability2RangeIndicator.enabled = false;

        // 모드 초기화
        isESkillLockOnMode = false;
    }

    /// <summary>
    /// E스킬 위치 설정 (고정 모드용)
    /// </summary>
    /// <param name="position">설정할 위치</param>
    private void SetESkillPosition(Vector3 position)
    {
        if (ability2Canvas == null) return;

        float fixedY = transform.position.y;
        ability2Canvas.transform.position = new Vector3(position.x, fixedY, position.z);
    }
}
