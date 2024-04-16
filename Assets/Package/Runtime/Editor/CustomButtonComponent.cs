#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CustomButton.Utils;
namespace CustomButton
{
    public abstract class CustomButtonComponent : EditorWindow
    {
        [MenuItem("GameObject/UI/Custom Button - TextMeshPro", false, 31)]
        private static void AddCustomButtonTMPro(MenuCommand menuCommand)
        {
            bool shouldDestroy = EditorUtility.DisplayDialog("Aviso", "Já existe uma instância do CustomButton. Deseja substituí-la?", "Sim", "Não");
            if (shouldDestroy)
            {
                // Destruir a instância mais recente
                CustomButtonBase[] existingButtons = FindObjectsOfType<CustomButtonBase>();
                if (existingButtons.Length > 0)
                {
                    DestroyImmediate(existingButtons[existingButtons.Length - 1].gameObject);
                }
            }
            GameObject obj = menuCommand.context as GameObject;
            RectTransform rectTransform = obj?.GetComponent<RectTransform>();
            Canvas canvas = FindCanvasInHierarchy(menuCommand);

            if (rectTransform != null)
            {

                if (canvas != null && RectTransformUtility.RectangleContainsScreenPoint(canvas.GetComponent<RectTransform>(), rectTransform.position))
                {
                    Debug.Log("Object is inside the Canvas.");
                    menuCommand.context = rectTransform.gameObject;
                }
                else
                {
                    Debug.Log("Object is outside the Canvas.");
                    menuCommand.context = canvas.gameObject;
                }
            }
            else
            {
                if (canvas == null)
                {
                    CreateCanvas(menuCommand);
                }
            }

            GameObject customButtonObject = new GameObject("Custom Button");
            GameObject textObject = new GameObject("Text (TMP)");

            EditorGUIUtility.PingObject(customButtonObject);
            EditorApplication.delayCall += () => Selection.activeGameObject = customButtonObject;

            RectTransform buttonObjectRT = customButtonObject.AddComponent<RectTransform>();
            RectTransform textRT = textObject.AddComponent<RectTransform>();
            
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            
            applySpriteButton(customButtonObject.TryGetComponent<Image>(out Image image) ? image : customButtonObject.AddComponent<Image>());
            buttonObjectRT.sizeDelta = new Vector2(160f, 30f);
            textRT.sizeDelta = Vector2.zero;

            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;

            textRT.sizeDelta = Vector2.zero;

            GameObject parentObject = menuCommand.context as GameObject;
            if (parentObject != null)
            {
                customButtonObject.transform.SetParent(parentObject.transform, false);
                textObject.transform.SetParent(customButtonObject.transform, false);
            }

            text.fontSize = 17;
            text.alignment = TextAlignmentOptions.Center;
            text.color = ColorUtility.TryParseHtmlString("#323232", out Color color) ? color : Color.black;
            text.SetText("Custom Button");
            
            CustomButtonClass custombutton = customButtonObject.AddComponent<CustomButtonClass>();
            string assetShake = "DefaultPresets/ShakePreset";
            AnimationPreset defaultPreset = Resources.Load<AnimationPreset>(assetShake);

            custombutton.animationPreset = defaultPreset;
            custombutton.TargetGraphic = customButtonObject.GetComponent<Image>();
            Undo.RegisterCreatedObjectUndo(customButtonObject, "Create " + customButtonObject.name);
            custombutton.OnTransformChildrenChanged();
            
        }
        static void applySpriteButton(Image image)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 7f;
            image.fillCenter = true;
            
            string assetPath = "Textures/texture_button_base";
            Sprite sprite = Resources.Load<Sprite>(assetPath);

            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }
        private static Canvas FindCanvasInHierarchy(MenuCommand menuCommand)
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas.isActiveAndEnabled)
                {
                    menuCommand.context = canvas.gameObject;
                    return canvas;
                }
            }
            return null;
        }
        private static void CreateCanvas(MenuCommand menuCommand)
        {
            GameObject canvasObject = new GameObject("Canvas");
            menuCommand.context = canvasObject;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        private bool CanAddCustomButton()
        {
            CustomButtonBase[] existingButtons = FindObjectsOfType<CustomButtonBase>();
            return existingButtons.Length < 1;
        }
        public static void ShowWindow()
        {
            GetWindow<CustomButtonComponent>("Custom Button Editor").Show();
        }
    }
    
}
#endif