using UnityEngine;
using UnityEngine.EventSystems;

public class SwordButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public SwordStyle style;
    public SwordStyleUIController controller;

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.ShowToolTip($"{style.styleName} 검술");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.HideToolTip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        controller.SelectSwordStyle(style);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
