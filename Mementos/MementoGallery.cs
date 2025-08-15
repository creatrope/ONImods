using UnityEngine;
using HarmonyLib;
using UnityEngine.UI;
using System.Collections.Generic;
using PeterHan.PLib.UI;

namespace Mementos
{
    [HarmonyPatch(typeof(TelepadSideScreen), "OnSpawn")]
    public static class TelepadSideScreen_AddBlankScreenButtonPatch
    {
        private static void Postfix(TelepadSideScreen __instance)
        {
            var summaryButton = __instance.viewColonySummaryBtn;
            if (summaryButton == null || summaryButton.gameObject == null || summaryButton.transform == null)
                return;

            var parent = summaryButton.transform.parent;
            if (parent == null)
                return;

            // Clone the summary button as a template
            var blankButtonObj = Object.Instantiate(summaryButton.gameObject, parent);
            blankButtonObj.name = "BlankScreenButton";
            blankButtonObj.transform.SetAsLastSibling();

            // Set button text
            var text = blankButtonObj.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = "Memento Gallery";
            var tmpText = blankButtonObj.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (tmpText != null)
                tmpText.text = "Memento Gallery";

            // Wire up click to open Plib modal
            var button = blankButtonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ShowPlibModal);
            }
            else
            {
                var kbutton = blankButtonObj.GetComponent<KButton>();
                if (kbutton != null)
                {
                    kbutton.ClearOnClick();
                    kbutton.onClick += ShowPlibModal;
                }
            }
        }

        private static void ShowPlibModal()
        {
            var dialog = new PDialog("MementoGalleryDialog")
            {
                Title = "Memento Gallery",
                Size = new Vector2(300, 250),
                MaxSize = new Vector2(300, 250),
                SortKey = 300.0f
            }
            .AddButton("ok", "OK", null, PUITuning.Colors.ButtonPinkStyle);

            // Use the dialog's Body as the main container
            var dialogBody = dialog.Body;

            // Use a panel with clearable children for easy refresh (optional)
            var dialogBodyChild = new PPanel("MementoGallery_RecordsPanel")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 4,
                FlexSize = Vector2.one
            };
            dialogBody.AddChild(dialogBodyChild);

            // Create the scrollable content panel
            var scrollBody = new PPanel("ScrollContent")
            {
                Spacing = 2,
                Direction = PanelDirection.Vertical,
                Alignment = TextAnchor.UpperCenter,
                FlexSize = Vector2.right
            };

            // Add your lines
            for (int i = 1; i <= 50; i++)
            {
                scrollBody.AddChild(new PLabel($"Line{i}") { Text = GetRandomLine(i) });
            }

            // Add the scroll pane
            var scrollPane = new PScrollPane()
            {
                ScrollHorizontal = false,
                ScrollVertical = true,
                Child = scrollBody,
                FlexSize = Vector2.one,
                TrackSize = 16f,
                AlwaysShowHorizontal = false,
                AlwaysShowVertical = true
            };
            dialogBodyChild.AddChild(scrollPane);

            dialog.Show();
        }

        private static string GetRandomLine(int i)
        {
            string[] samples = new[]
            {
                "The quick brown fox jumps over the lazy dog.",
                "Lorem ipsum dolor sit amet.",
                "Random value: " + Random.Range(1000, 9999),
                "Unity modding is fun!",
                "Hello, world!",
                "Sample text line.",
                "Another random thought.",
                "ONI rocks!",
                "Plib makes UI easy.",
                "Test entry."
            };
            // Pick a random sample and append the line number
            var sample = samples[Random.Range(0, samples.Length)];
            return $"Line {i}: {sample}";
        }
    }
}