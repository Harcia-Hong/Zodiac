using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int curHealth;
    public Slider hpSlider;

    Animator anim;
    public bool isDead = false;

    private void Awake()
    {
        curHealth = maxHealth;
        anim = GetComponentInChildren<Animator>();
        
        if(hpSlider != null)
        {
            hpSlider.maxValue = maxHealth;
        }
    }

    private void Update()
    {

    }
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        curHealth -= Mathf.RoundToInt(damage);
        curHealth = Mathf.Clamp(curHealth, 0, maxHealth); // - 를 통한 음수 방지

        // DOTween 애니메이션 적용
        if (hpSlider != null)
        {
            hpSlider.DOValue(curHealth, 0.3f).SetEase(Ease.OutQuad); // SetEase : 빠르게 시작해서, 천천히 감속하는 감속 속선, hp가 부드럽게 감소하는 느낌 연출
        }

        if (curHealth > 0)
        // anim.SetTrigger("doHit");피격 애니메이션 삭제
            { }
        else
            Die();
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("doDie");
    }
}
