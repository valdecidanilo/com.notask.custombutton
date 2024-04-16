using UnityEngine.EventSystems;

namespace CustomButton
{
    public interface ICustomButton
    {
        public virtual void OnClick(){}
        public virtual void OnPointerClick(PointerEventData eventData){}
        public virtual void OnPointerDown(PointerEventData eventData){}
        public virtual void OnPointerUp(PointerEventData eventData){}
        public virtual void OnPointerEnter(PointerEventData eventData){}
        public virtual void OnPointerExit(PointerEventData eventData){}
    }
}