using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CustomButton.Utils;
namespace CustomButton
{
    //[ExecuteAlways]
    [RequireComponent(typeof(Image)), ExecuteInEditMode]
    public abstract class CustomButtonBase : MonoBehaviour, ICustomButton, ISubmitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        public AnimationPreset animationPreset;
        [SerializeField]
        private bool _interactable = true;
        private bool interactable
        {
            get { return _interactable; }
            set
            {
                if (_interactable == value) return;
                _interactable = value;
                UpdateButtonState();
            }
        }
        [SerializeField]
        private VisibilityMode transition = VisibilityMode.ColorTint;
        [SerializeField]
        private Graphic targetGraphic;
        [System.Serializable]
        public struct ColorBlockCustom
        {
            public Color normalColor;
            public Color highlightedColor;
            public Color pressedColor;
            public Color selectedColor;
            public Color disabledColor;
            [Range(0f, 1f)] public float colorMultiplier;
            public float fadeDuration;

            private void DefaultColor()
            {
                normalColor      = new Color32(255, 255, 255, 255);
                highlightedColor = new Color32(245, 245, 245, 255);
                pressedColor     = new Color32(200, 200, 200, 255);
                selectedColor    = new Color32(245, 245, 245, 255);
                disabledColor    = new Color32(200, 200, 200, 128);
                colorMultiplier    = 1.0f;
                fadeDuration       = 0.1f;
            }
        }
        [SerializeField]
        private ColorBlock colors = ColorBlock.defaultColorBlock;
        [SerializeField]
        private Color previousInvertedTextColor;
        [SerializeField]
        private Color invertedCurrentColor = Color.white;
        [SerializeField]
        private SpriteState spriteState = new SpriteState();
        private Sprite previousSprite;
        private Coroutine colorLerpCoroutine;
        private Coroutine opacityLerpCoroutine;
        private Coroutine animationCoroutine;
        private Coroutine offsetCoroutine;
        [SerializeField]
        public Graphic[] graphics;
        [SerializeField]
        private bool invertTextColor;

        private bool isActiveColorTint;
        private bool isActiveSpriteSwap;
        private bool isActiveAnimation;
        
        public bool isOffsetClick;
        [Range(0f, 0.4f)] public float durationOffset = 0.1f;
        public Vector2 offsetVectorClick;
        private Vector2[] initialPositions = new Vector2[0];
        
        private bool isPressed;
        public Graphic TargetGraphic
        {
            get { return targetGraphic = GetComponent<Graphic>(); }
            set { targetGraphic = value; }
        }

        public ColorBlock Colors
        {
            get { return colors; }
            set { colors = value; }
        }
        public Button.ButtonClickedEvent onClick = new ();

        public SpriteState SpriteState
        {
            get { return spriteState; }
            set { spriteState = value; }
        }

        private void Start()
        {
            onClick.AddListener(OnClick);
            
            GetChildren();
        }
        private void OnDestroy() {
            onClick.RemoveAllListeners();
            TargetGraphic = null;
        }
        public void SetInteractable(bool active)
        {
            interactable = active;
        }
        public void ToogleInteractable()
        {
            interactable = !interactable;
        }
        public virtual void OnClick()
        {
            if(!interactable) return;
            if (transition != VisibilityMode.Animation && !isActiveAnimation) return;
            if(animationCoroutine != null || animationPreset == null) return;
            animationCoroutine = animationPreset.animationStyle switch
            {
                AnimationStyle.Shake => StartCoroutine(ShakeAnimation()),
                AnimationStyle.Scale => StartCoroutine(ScaleAnimation()),
                AnimationStyle.Rotate => StartCoroutine(RotateAnimation()),
                _ => animationCoroutine
            };
        }
        public void OnTransformChildrenChanged()
        {
            GetChildren();
            UpdateButtonState();
        }

        private void GetChildren()
        {
            graphics = GetComponentsInChildren<Graphic>();
            //Add button to graphics to button in editor
        }
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            onClick?.Invoke();
        }
        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            if(!interactable) return;
            if(transition == VisibilityMode.ColorTint || isActiveColorTint){
                if (colorLerpCoroutine != null)
                {
                    StopCoroutine(colorLerpCoroutine);
                }

                colorLerpCoroutine = StartCoroutine(
                    SmoothColorLerpCoroutine(
                        targetGraphic.color,
                        colors.highlightedColor,
                        colors.fadeDuration));
            }

            if (transition != VisibilityMode.SpriteSwap && !isActiveSpriteSwap) return;
            var targetImage = targetGraphic as Image;
            previousSprite = targetImage.sprite;
            targetImage.sprite = spriteState.highlightedSprite;
        }
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            if(!interactable) return;
            if(transition == VisibilityMode.ColorTint || isActiveColorTint)
            {
                if (colorLerpCoroutine != null)
                {
                    StopCoroutine(colorLerpCoroutine);
                }

                colorLerpCoroutine = StartCoroutine(
                    SmoothColorLerpCoroutine(
                        targetGraphic.color,
                        colors.normalColor,
                        colors.fadeDuration));
            }
            if(isOffsetClick && initialPositions.Length > 0)
                offsetCoroutine = StartCoroutine(OffsetBalanceUp(initialPositions, durationOffset));
            if (transition != VisibilityMode.SpriteSwap && !isActiveSpriteSwap) return;
            var targetImage = targetGraphic as Image;
            targetImage.sprite = previousSprite;
        }
        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if(!interactable) return;
            isPressed = true;
            if (isOffsetClick && offsetCoroutine == null)
            {
                offsetCoroutine = StartCoroutine(OffsetBalanceDown(offsetVectorClick, durationOffset));
            }
            UpdateButtonState();
        }
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if(!interactable) return;
            isPressed = false;
            if (isOffsetClick)
            {
                offsetCoroutine = StartCoroutine(OffsetBalanceUp(initialPositions, durationOffset));
            }
            UpdateButtonState();
        }
        private void UpdateButtonState()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Image>();
            }
            if(transition == VisibilityMode.ColorTint || isActiveColorTint)
            {
                if (isPressed)
                {
                    colorLerpCoroutine = StartCoroutine(
                        SmoothColorLerpCoroutine(
                            targetGraphic.color, 
                            colors.pressedColor, 
                            colors.fadeDuration));

                    opacityLerpCoroutine = StartCoroutine(
                        SmoothOpacityLerpCoroutine2(
                            graphics,
                            colors.pressedColor,
                            colors.fadeDuration));

                            return;
                }
                SetColorInteractable();
            }
            if (transition == VisibilityMode.SpriteSwap || isActiveSpriteSwap)
            {
                Debug.Log("Sprite Swap");
                var targetImage = targetGraphic as Image;
                
                if (isPressed)
                {
                    Debug.Log("Pressed");
                    targetImage.sprite = spriteState.pressedSprite != null ? spriteState.pressedSprite : targetImage.sprite;
                }
                else if (interactable)
                {
                    Debug.Log("Normal");
                    targetImage.sprite = spriteState.highlightedSprite != null ? spriteState.highlightedSprite : targetImage.sprite;
                }
                else
                {
                    Debug.Log("Disabled");
                    targetImage.sprite = spriteState.disabledSprite != null ? spriteState.disabledSprite : targetImage.sprite;
                }
            }

            if (transition != VisibilityMode.Animation && !isActiveAnimation && !isPressed) return;
            //if (!isPressed) return;
            if(animationPreset == null)
                Debug.LogWarning($"[CustomButton] Animation preset is null in {gameObject.name}");
        }
        public void CheckInverterColorText()
        {
            if(!invertTextColor)
            {
                if(previousInvertedTextColor != invertedCurrentColor)
                {
                    UpdateInverterTextColor(previousInvertedTextColor);
                }
                return;
            }
            var normalColor = (Colors.normalColor);
                invertedCurrentColor = new Color(
                    1f - normalColor.r,
                    1f - normalColor.g, 
                    1f - normalColor.b, 
                    normalColor.a);
                UpdateInverterTextColor(invertedCurrentColor);
        }
        private void UpdateInverterTextColor(Color currentColor)
        {
            var texts = GetComponentsInChildren<TMP_Text>();
            foreach (var text in texts)
            {
                text.color = currentColor;
            }
        }
        public void SetColorInteractable()
        {
            SetColorChildren();
            colorLerpCoroutine = StartCoroutine(
                SmoothColorLerpCoroutine(
                    targetGraphic.color, 
                    interactable ? colors.normalColor : colors.disabledColor, 
                    colors.fadeDuration));
        }
        public void SetColorChildren()
        {
            Color targetColor;
            if(interactable)
            {
                targetColor = colors.normalColor;
            }
            else
            {
                targetColor = colors.disabledColor;
            }

            opacityLerpCoroutine = StartCoroutine(
                SmoothOpacityLerp(
                    graphics,
                    targetColor,
                    colors.fadeDuration));
        }
        private IEnumerator ShakeAnimation()
        {
            var elapsedTime = 0f;
            var rectTransform = GetComponent<RectTransform>();
            Vector3 originalPosition = rectTransform.anchoredPosition;
            while (elapsedTime < animationPreset.duration)
            {
                var x = originalPosition.x + Mathf.Sin(Time.time * animationPreset.speed) * animationPreset.magnitude;
                var y = originalPosition.y + Mathf.Cos(Time.time * animationPreset.speed) * animationPreset.magnitude;

                rectTransform.anchoredPosition = new Vector3(x, y, originalPosition.z);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            animationCoroutine = null;
            rectTransform.anchoredPosition = originalPosition;
        }
        private IEnumerator ScaleAnimation()
        {
            var elapsedTime = 0f;
            var rectTransform = GetComponent<RectTransform>();
            var originalScale = rectTransform.localScale;

            while (elapsedTime < animationPreset.duration)
            {
                var t = elapsedTime / animationPreset.duration;
                transform.localScale = Vector3.Lerp(Vector3.one * animationPreset.magnitude, originalScale, t);

                elapsedTime += Time.deltaTime * animationPreset.speed;
                yield return null;
            }

            animationCoroutine = null;
            rectTransform.localScale = originalScale;
        }
        private IEnumerator RotateAnimation()
        {
            var elapsedTime = 0f;
            var rectTransform = GetComponent<RectTransform>();
            var originalRotation = rectTransform.rotation;

            while (elapsedTime < animationPreset.duration)
            {
                var rotationAmount = Mathf.Sin(Time.time * animationPreset.speed) * animationPreset.magnitude;
                var rotation = originalRotation * Quaternion.Euler(0f, 0f, rotationAmount);

                rectTransform.rotation = rotation;

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            animationCoroutine = null;
            rectTransform.rotation = originalRotation;
        }
        private IEnumerator SmoothColorLerpCoroutine(Color startColor, Color targetColor, float duration)
        {
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var t = Mathf.Clamp01(elapsedTime / duration);

                //Lerp Color minus Opacity
                /*
                targetGraphic.color = new Color(
                    Mathf.Lerp(startColor.r, targetColor.r, t),
                    Mathf.Lerp(startColor.g, targetColor.g, t),
                    Mathf.Lerp(startColor.b, targetColor.b, t),
                    targetGraphic.color.a);
                */
                targetGraphic.color = Color.Lerp(startColor, targetColor, t);

                yield return null;
            }
        }
        private static IEnumerator SmoothOpacityLerpCoroutine2(IReadOnlyList<Graphic> graphics, Color targetColor, float duration)
        {
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var t = Mathf.Clamp01(elapsedTime / duration);

                for (var i = 1; i < graphics.Count; i++)
                {
                    var currentgraphic = graphics[i];


                    /* Only change alpha */

                    currentgraphic.color = new Color(
                        currentgraphic.color.r,
                        currentgraphic.color.g,
                        currentgraphic.color.b,
                        Mathf.Lerp(currentgraphic.color.a, targetColor.a, t));
                }

                yield return null;
            }
        }

        private IEnumerator OffsetBalanceUp(Vector2[] initialPositions, float duration)
        {
            var elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                var t = Mathf.Clamp01(elapsedTime / duration);

                for (var i = 0; i < graphics.Length; i++)
                {
                    var currentGraphic = graphics[i];
                    var currentInitialPosition = initialPositions[i];

                    currentGraphic.rectTransform.anchoredPosition = Vector2.Lerp(currentGraphic.rectTransform.anchoredPosition, currentInitialPosition, t);
                }

                yield return null;
            }
            for (var i = 0; i < graphics.Length; i++)
            {
                graphics[i].rectTransform.anchoredPosition = initialPositions[i];
            }
            offsetCoroutine = null;
        }
        private IEnumerator OffsetBalanceDown(Vector2 offset, float duration)
        {
            initialPositions = new Vector2[graphics.Length];
            for (var i = 0; i < graphics.Length; i++)
            {
                initialPositions[i] = graphics[i].rectTransform.anchoredPosition;
            }

            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                var t = Mathf.Clamp01(elapsedTime / duration);

                for (var i = 0; i < graphics.Length; i++)
                {
                    var currentGraphic = graphics[i];
                    var currentInitialPosition = initialPositions[i];
                    var targetPosition = currentInitialPosition + offset;
                    
                    currentGraphic.rectTransform.anchoredPosition = Vector2.Lerp(currentInitialPosition, targetPosition, t);
                }

                yield return null;
            }

            for (var i = 0; i < graphics.Length; i++)
            {
                graphics[i].rectTransform.anchoredPosition = initialPositions[i] + offset;
            }
        }
        
        private IEnumerator SmoothOpacityLerp(Graphic[] startColor, Color targetColor, float duration)
        {
            var elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var t = Mathf.Clamp01(elapsedTime / duration);
                
                for (var i = 1; i < graphics.Length; i++)
                {
                    var currentgraphic = graphics[i];

                    /* Only change alpha */

                    currentgraphic.color = new Color(
                        currentgraphic.color.r,
                        currentgraphic.color.g,
                        currentgraphic.color.b,
                        Mathf.Lerp(startColor[i].color.a, targetColor.a, t));
                }
            }
            yield return null;
        }

        public void OnSubmit(BaseEventData eventData)
        {

        }
    }
}