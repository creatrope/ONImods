using PeterHan.PLib.UI;
using TMPro;
using UnityEngine;
using System.Collections.Generic; // Add this namespace for Dictionary<,>

namespace SensorsPlus
{
    public class MyThresholdSwitch
    {
        private const string AboveLabel = "Above";
        private const string BelowLabel = "Below";

        private readonly string fieldId;
        private readonly string labelText;
        private readonly string defaultValue;

        private PTextField inputField;
        private TMP_InputField unityInputField;
        private PLabel outputField;
        private LocText outputLocText;
        private IThresholdSwitchState stateComponent; // Use IThresholdSwitchState for generality; cast as needed

        private GameObject parentForBuild = null;

        private PButton aButton;
        private PButton bButton;
        private UnityEngine.UI.Button unityAButton;
        private UnityEngine.UI.Button unityBButton;
        private KButton kAButton;
        private KButton kBButton;
        private bool isAButtonPressed = false;
        private bool isBButtonPressed = false;
        private bool isAButtonInteractable = true;
        private bool isBButtonInteractable = true;

        private static readonly Color ButtonOnColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        private static readonly Color ButtonOffColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        public int OutputBit { get; }

        public string FieldId => fieldId;
        public bool IsAButtonPressed => isAButtonPressed;
        public bool IsBButtonPressed => isBButtonPressed;
        public bool IsAButtonInteractable => isAButtonInteractable;
        public bool IsBButtonInteractable => isBButtonInteractable;
        public PTextField InputField => inputField;

        public MyThresholdSwitch(string id, string label, string defaultValue = "1.0", int outputBit = 0)
        {
            this.fieldId = id;
            this.labelText = label;
            this.defaultValue = defaultValue;
            this.OutputBit = outputBit;
        }

        public void SetParentForBuild(GameObject parent)
        {
            parentForBuild = parent;
        }

        private PButton CreateButton(string buttonId, string label, System.Action onToggle, out UnityEngine.UI.Button unityButtonRef, out KButton kButtonRef)
        {
            UnityEngine.UI.Button localButtonRef = null;
            KButton localKButtonRef = null;
            string displayLabel = label;
            if (buttonId == "A") displayLabel = AboveLabel;
            else if (buttonId == "B") displayLabel = BelowLabel;
            var pButton = new PButton($"{buttonId}Button_{fieldId}")
            {
                Text = displayLabel,
                TextStyle = PUITuning.Fonts.TextLightStyle,
                ToolTip = $"Toggle {displayLabel}",
                FlexSize = new Vector2(22, 22),
                OnClick = (buttonSource) =>
                {
                    onToggle?.Invoke();
                    if (localKButtonRef != null)
                        localKButtonRef.isInteractable = (buttonId == "A" ? isAButtonInteractable : isBButtonInteractable);
                    UpdateButtonVisual();
                }
            };
            pButton.AddOnRealize(realizedGo =>
            {
                localButtonRef = realizedGo.GetComponentInChildren<UnityEngine.UI.Button>();
                localKButtonRef = realizedGo.GetComponent<KButton>();
                if (buttonId == "A") { unityAButton = localButtonRef; kAButton = localKButtonRef; }
                else if (buttonId == "B") { unityBButton = localButtonRef; kBButton = localKButtonRef; }
                UpdateButtonVisual();
            });
            unityButtonRef = localButtonRef;
            kButtonRef = localKButtonRef;
            return pButton;
        }

        public GameObject Build(GameObject parent)
        {
            var row = new PPanel("RowPanel_" + fieldId)
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 5
            };

            row.AddChild(new PLabel("Label_" + fieldId)
            {
                Text = labelText,
                TextStyle = PUITuning.Fonts.TextDarkStyle
            });

            aButton = CreateButton("A", AboveLabel, OnAButtonClicked, out unityAButton, out kAButton);
            row.AddChild(aButton);

            bButton = CreateButton("B", BelowLabel, OnBButtonClicked, out unityBButton, out kBButton);
            row.AddChild(bButton);

            inputField = new PTextField("InputField_" + fieldId)
            {
                Text = defaultValue,
                MinWidth = 60,
                OnTextChanged = (source, val) => {
                    if (stateComponent != null) {
                        stateComponent.CustomFields[fieldId] = val;
                    }
                }
            }
            .AddOnRealize(realizedGo => {
                unityInputField = realizedGo.GetComponentInChildren<TMP_InputField>();
            });
            row.AddChild(inputField);

            outputField = new PLabel("OutputField_" + fieldId)
            {
                Text = "00000.00",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(realizedGo => {
                outputLocText = realizedGo.transform.Find("Text")?.GetComponent<LocText>();
            });
            row.AddChild(outputField);

            return row.AddTo(parent);
        }

        public GameObject BuildUIRow(GameObject parent)
        {
            var row = new PPanel("RowPanel_" + fieldId)
            {
                Direction = PanelDirection.Horizontal,
                Spacing = 5
            };

            row.AddChild(new PLabel("Label_" + fieldId)
            {
                Text = labelText,
                TextStyle = PUITuning.Fonts.TextDarkStyle
            });

            aButton = CreateButton("A", AboveLabel, OnAButtonClicked, out unityAButton, out kAButton);
            row.AddChild(aButton);

            bButton = CreateButton("B", BelowLabel, OnBButtonClicked, out unityBButton, out kBButton);
            row.AddChild(bButton);

            inputField = new PTextField("InputField_" + fieldId)
            {
                Text = defaultValue,
                MinWidth = 60,
                OnTextChanged = (source, val) => {
                    if (stateComponent != null) {
                        stateComponent.CustomFields[fieldId] = val;
                    }
                }
            }
            .AddOnRealize(realizedGo => {
                unityInputField = realizedGo.GetComponentInChildren<TMP_InputField>();
            });
            row.AddChild(inputField);

            outputField = new PLabel("OutputField_" + fieldId)
            {
                Text = "00000.00",
                TextStyle = PUITuning.Fonts.TextDarkStyle
            }.AddOnRealize(realizedGo => {
                outputLocText = realizedGo.transform.Find("Text")?.GetComponent<LocText>();
            });
            row.AddChild(outputField);

            return row.AddTo(parent);
        }

        public void SetTarget(object state)
        {
            stateComponent = state as IThresholdSwitchState;
            if (stateComponent != null)
            {
                // Restore input field value, or set default if not present
                string valueToSet = defaultValue;
                if (stateComponent.CustomFields != null && stateComponent.CustomFields.TryGetValue(fieldId, out var savedValue))
                {
                    valueToSet = savedValue;
                }
                else
                {
                    if (stateComponent.CustomFields == null)
                        stateComponent.CustomFields = new Dictionary<string, string>();
                    stateComponent.CustomFields[fieldId] = defaultValue;
                }
                if (unityInputField != null)
                    unityInputField.text = valueToSet;

                // Restore button states, or set defaults if not present
                bool hasA = false, hasB = false;
                if (stateComponent.ButtonStates != null)
                {
                    if (stateComponent.ButtonStates.TryGetValue(fieldId + "_A", out var aState))
                    {
                        isAButtonPressed = aState;
                        hasA = true;
                    }
                    if (stateComponent.ButtonStates.TryGetValue(fieldId + "_B", out var bState))
                    {
                        isBButtonPressed = bState;
                        hasB = true;
                    }
                }
                if (!hasA && !hasB)
                {
                    isAButtonPressed = true;
                    isBButtonPressed = false;
                    if (stateComponent.ButtonStates == null)
                        stateComponent.ButtonStates = new Dictionary<string, bool>();
                    stateComponent.ButtonStates[fieldId + "_A"] = true;
                    stateComponent.ButtonStates[fieldId + "_B"] = false;
                }

                // Always refresh interactable state
                isAButtonInteractable = !isAButtonPressed;
                isBButtonInteractable = !isBButtonPressed;

                UpdateButtonVisual();
            }
        }

        private void SaveState()
        {
            if (stateComponent != null)
            {
                if (stateComponent.CustomFields == null)
                    stateComponent.CustomFields = new Dictionary<string, string>();
                stateComponent.CustomFields[fieldId] = unityInputField?.text ?? defaultValue;

                if (stateComponent.ButtonStates == null)
                    stateComponent.ButtonStates = new Dictionary<string, bool>();
                stateComponent.ButtonStates[fieldId + "_A"] = IsAButtonPressed;
                stateComponent.ButtonStates[fieldId + "_B"] = IsBButtonPressed;
            }
        }

        private void UpdateButtonVisual()
        {
            if (aButton != null)
                aButton.Text = isAButtonPressed ? AboveLabel : AboveLabel;
            if (bButton != null)
                bButton.Text = isBButtonPressed ? BelowLabel : BelowLabel;

            if (unityAButton != null)
                unityAButton.image.color = isAButtonPressed ? ButtonOnColor : ButtonOffColor;
            if (unityBButton != null)
                unityBButton.image.color = isBButtonPressed ? ButtonOnColor : ButtonOffColor;

            if (kAButton != null)
                kAButton.isInteractable = isAButtonInteractable;
            if (kBButton != null)
                kBButton.isInteractable = isBButtonInteractable;
        }

        public void UpdateOutput()
        {
            float val = 0f;
            if (stateComponent != null)
            {
                val = stateComponent.GetValue(fieldId);
            }

            if (outputLocText != null)
                outputLocText.text = val.ToString("00000.00");
        }

        public bool GetSignalOn()
        {
            if (stateComponent == null)
            {
                return false;
            }

            float outputVal = stateComponent.GetValue(fieldId);

            float inputVal = 0f;
            if (inputField != null && float.TryParse(inputField.Text, out float parsed))
                inputVal = parsed;

            bool above = isAButtonPressed && outputVal > inputVal;
            bool below = isBButtonPressed && outputVal < inputVal;
            bool result = above || below;

            return result;
        }

        private void OnAButtonClicked()
        {
            isAButtonPressed = !isAButtonPressed;
            isAButtonInteractable = false;
            isBButtonPressed = false;
            isBButtonInteractable = true;
            UpdateButtonVisual();
            SaveState();
        }

        private void OnBButtonClicked()
        {
            isBButtonPressed = !isBButtonPressed;
            isBButtonInteractable = false;
            isAButtonPressed = false;
            isAButtonInteractable = true;
            UpdateButtonVisual();
            SaveState();
        }
    }
}