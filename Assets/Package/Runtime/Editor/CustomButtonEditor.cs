#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using CustomButton.Utils;
namespace CustomButton
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CustomButtonClass))]
    public class CustomButtonEditor : Editor
    {
        private const string SelectedTabPreferenceKeyPrefix = "SelectedTabPreferenceKey_";
        
        SerializedProperty m_InteractableProperty;
        SerializedProperty m_TargetGraphicProperty;
        SerializedProperty m_TransitionProperty;
        SerializedProperty onClickProperty;
        SerializedProperty colorsProperty;
        SerializedProperty colorMultiplierProperty;
        SerializedProperty fadeDurationProperty;
        SerializedProperty spriteStateProperty;
        SerializedProperty customButtonPresetProperty;
        [Tooltip("Inverts the text color according to the Normal Color")]
        SerializedProperty invertTextColorProperty;
        SerializedProperty m_isOffsetClickProperty;
        SerializedProperty OffsetVectorProperty;
        SerializedProperty OffsetDurationProperty;
        SerializedProperty invertTextCustomColorProperty;
        SerializedProperty invertedCurrentColorProperty;

        private SerializedProperty graphicsChildrenProperty;

        private Texture2D customIcon;

        private int selectedTab = 1;

        private bool isOffsetClick;
        private bool isTextColorInverter;
        private bool showColorGroup;
        private bool showColorProperties;
        private bool showSpriteProperties;
        private bool showAnimationProperties;
        
        private bool previousInteractableValue;
        private Color previousNormalColor;

        private void OnEnable()
        {
            CustomButtonClass cb = null;
            /*if (((CustomButtonClass)target).TryGetComponent<CustomButtonClass>(out cb))
            {
                CustomButtonClass[] customButtonComponents = cb.GetComponents<CustomButtonClass>();
                if (customButtonComponents.Length > 1)
                {
                    // Exibe um modal para informar ao usuário que o script está duplicado
                    EditorUtility.DisplayDialog("Duplicate Script",
                        "The object contains more than one component <CustomButton>.", "Ok");

                    if (!EditorUtility.IsPersistent(customButtonComponents[1]))
                    {
                        DestroyImmediate(customButtonComponents[1]);
                        Debug.Log("Duplicate CustomButtonClass component removed.");
                    }
                }
            }*/

            string assetPath = "Icons/icon_customButton";
            Texture2D sprite = Resources.Load<Texture2D>(assetPath);
            customIcon = sprite;

            onClickProperty = serializedObject.FindProperty("onClick");

            m_InteractableProperty = serializedObject.FindProperty("_interactable");
            previousInteractableValue = m_InteractableProperty.boolValue;

            m_TargetGraphicProperty = serializedObject.FindProperty("targetGraphic");
            colorsProperty = serializedObject.FindProperty("colors");
            spriteStateProperty = serializedObject.FindProperty("spriteState");
            m_TransitionProperty = serializedObject.FindProperty("transition");
            customButtonPresetProperty = serializedObject.FindProperty("animationPreset");
            graphicsChildrenProperty = serializedObject.FindProperty("graphics");
            invertedCurrentColorProperty = serializedObject.FindProperty("invertedCurrentColor");
            invertTextCustomColorProperty = serializedObject.FindProperty("invertTextCustomColor");
            invertTextColorProperty = serializedObject.FindProperty("invertTextColor");
            
            m_isOffsetClickProperty = serializedObject.FindProperty("isOffsetClick");
            OffsetVectorProperty = serializedObject.FindProperty("offsetVectorClick");
            OffsetDurationProperty = serializedObject.FindProperty("durationOffset");
            isOffsetClick = m_isOffsetClickProperty.boolValue;
            
            selectedTab = EditorPrefs.GetInt(GetGameObjectKey(target), 1);
            SelectTab(selectedTab);
            
            PrefabUtility.prefabInstanceUpdated += PrefabInstanceUpdatedCallback;
            EditorApplication.hierarchyChanged += HierarchyChangedCallback;

            previousNormalColor = ((CustomButtonClass)target).Colors.normalColor;
        }
        private string GetGameObjectKey(Object targetObject)
        {
            return SelectedTabPreferenceKeyPrefix + targetObject.name + "_" + targetObject.GetType().Name;
        }
        private void OnDisable()
        {
            // Remove os listeners dos eventos
            PrefabUtility.prefabInstanceUpdated -= PrefabInstanceUpdatedCallback;
            EditorApplication.hierarchyChanged -= HierarchyChangedCallback;
        }

        private void PrefabInstanceUpdatedCallback(GameObject instance)
        {
            // Verifica se o objeto atual é uma instância do CustomButtonClass
            if (instance.TryGetComponent(out CustomButtonClass customButton))
            {
                // Lógica a ser executada quando uma instância do CustomButtonClass é atualizada
                Debug.Log("Instância do CustomButtonClass atualizada: " + customButton.gameObject.name);
            }
        }

        private void HierarchyChangedCallback()
        {
            // Verifica se o objeto alvo deste editor ainda existe na hierarquia
            if (target != null && ((CustomButtonClass)target).gameObject != null)
            {
                // Lógica a ser executada quando a hierarquia é alterada
                Debug.Log("HierarchyChangedCallback " + ((CustomButtonClass)target).gameObject.name);
            }
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            CustomButtonBase customButtonBase = (CustomButtonBase)target;

            EditorGUIUtility.SetIconForObject(target, customIcon); 

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            bool interactableValue = EditorGUILayout.Toggle("Interactable", m_InteractableProperty.boolValue);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(customButtonBase, "Change Interactable");
                m_InteractableProperty.boolValue = interactableValue;
                m_InteractableProperty.serializedObject.ApplyModifiedProperties();
                customButtonBase.Invoke(nameof(customButtonBase.SetColorInteractable), 0.1f);
                previousInteractableValue = interactableValue;
                SceneView.RepaintAll();

                EditorUtility.SetDirty(customButtonBase);
            }
    
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginChangeCheck();
            bool setClickValue = EditorGUILayout.Toggle("Is Offset Click", m_isOffsetClickProperty.boolValue);
            if (EditorGUI.EndChangeCheck()) 
            {
                Undo.RecordObject(customButtonBase, "Change Set Click");
                m_isOffsetClickProperty.boolValue = setClickValue;
                m_isOffsetClickProperty.serializedObject.ApplyModifiedProperties();
                isOffsetClick = setClickValue;
                EditorUtility.SetDirty(customButtonBase);
            }

            if (setClickValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(OffsetVectorProperty, true);
                EditorGUILayout.PropertyField(OffsetDurationProperty, true);
                EditorGUI.indentLevel--;
            }
            
            string[] tabs = { "None", "Color Tint", "Sprite Swap", "Animation" };
            selectedTab = GUILayout.Toolbar(selectedTab, tabs);
            if (selectedTab != EditorPrefs.GetInt(GetGameObjectKey(target), 1))
            {
                EditorPrefs.SetInt(GetGameObjectKey(target), selectedTab);
                SelectTab(selectedTab);
            }
            if (showColorProperties)
            {
                //EditorGUI.indentLevel++;
                
                EditorGUI.BeginChangeCheck();
                
                
                //EditorGUI.BeginChangeCheck();
                /*isTextColorInverter = EditorGUILayout.Toggle("Custom Color Text", invertTextCustomColorProperty.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    
                    invertTextCustomColorProperty.boolValue = isCustomColorInverter;
                    invertTextCustomColorProperty.serializedObject.ApplyModifiedProperties();
                }*/
                EditorGUILayout.PropertyField(m_TargetGraphicProperty);
                showColorGroup = EditorGUILayout.BeginFoldoutHeaderGroup(showColorGroup, "Colors");
                if (showColorGroup)
                {
                    EditorGUILayout.PropertyField(colorsProperty, GUIContent.none, true);
                }
                isTextColorInverter = EditorGUILayout.Toggle("Invert Text Color", invertTextColorProperty.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(customButtonBase, "Change Invert Text Color");
                    invertTextColorProperty.boolValue = isTextColorInverter;
                    invertTextColorProperty.serializedObject.ApplyModifiedProperties();

                    EditorUtility.SetDirty(customButtonBase);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                if (previousNormalColor != customButtonBase.Colors.normalColor)
                {
                    previousNormalColor = customButtonBase.Colors.normalColor;
                    EditorApplication.update += UpdateTargetGraphicColor;
                }
                if (invertTextColorProperty.boolValue)
                {
                    /*if(invertTextCustomColorProperty.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.ColorField("Inverted Current Color", invertedCurrentColorProperty.colorValue);
                        EditorApplication.update += UpdateTargetGraphicColor;
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        customButtonBase.CheckInverterColorText();
                    }
                    if(!invertTextCustomColorProperty.boolValue)
                    {
                        customButtonBase.CheckInverterColorText();
                    }*/
                }
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUI.indentLevel--;
            if (showSpriteProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_TargetGraphicProperty);
                if(m_TargetGraphicProperty.objectReferenceValue.GetType().Equals(typeof(UnityEngine.UI.Image)) || m_TargetGraphicProperty.objectReferenceValue.GetType() != null)
                {
                    EditorGUILayout.PropertyField(spriteStateProperty, true);
                }
                else
                    EditorGUILayout.HelpBox("Target Graphic must be an Image", MessageType.Warning);
            }

            if (showAnimationProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(customButtonPresetProperty, true);
                if(customButtonPresetProperty.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Animation there has to be a preset! \n Create one in Asset/Create/CustomButton/Preset and new Preset Animation", MessageType.Warning);
                }
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            EditorGUI.indentLevel++;

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(onClickProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private void SelectTab(int tab)
        {
            switch (tab)
            {
                case 0: // None
                    m_TransitionProperty.enumValueIndex = 0;
                    showSpriteProperties = false;
                    showColorProperties = false;
                    showAnimationProperties = false;
                    break;
                case 1: // Color Tint
                    m_TransitionProperty.enumValueIndex = 1;
                    showColorProperties = true;
                    showSpriteProperties = false;
                    showAnimationProperties = false;
                    break;
                case 2: // Sprite Swap
                    m_TransitionProperty.enumValueIndex = 2;
                    showColorProperties = false;
                    showSpriteProperties = true;
                    showAnimationProperties = false;
                    break;
                case 3: // Animation
                    m_TransitionProperty.enumValueIndex = 3;
                    showColorProperties = false;
                    showSpriteProperties = false;
                    showAnimationProperties = true;
                    break;
            }
            serializedObject.ApplyModifiedProperties();
        }

        public static void LogSelectionExample()
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Debug.Log("GameObject created: " + obj.name);

            EditorGUIUtility.PingObject(obj);
            EditorApplication.delayCall += () => Selection.activeGameObject = obj;
        }
        private void UpdateTargetGraphicColor()
        {
            CustomButtonClass customButton = (CustomButtonClass)target;
            if(customButton != null)
            {
                //customButton.TargetGraphic.color = customButton.Colors.normalColor;
            }
        }
    }
}
#endif