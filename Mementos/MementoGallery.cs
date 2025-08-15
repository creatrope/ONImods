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
            var dialog = new PDialog("TestModal");
            dialog.Title = "Test Modal";
            dialog.Size = new Vector2(400, 200); // Locks the dialog window size

            // Create a vertical layout for the lines
            var vbox = new PPanel("VBox")
            {
                Direction = PanelDirection.Vertical,
                Spacing = 2,
                DynamicSize = false // Prevents the panel from expanding to fit all children
            };

            var scrollPane = new PScrollPane("ScrollPane")
            {
                Child = vbox,
                ScrollVertical = true,
                ScrollHorizontal = false,
                TrackSize = 16f,
                FlexSize = new Vector2(0, 18 * 5),
                AlwaysShowVertical = true
            };


            for (int i = 1; i <= 50; i++)
            {
                vbox.AddChild(new PLabel($"Line{i}") { Text = GetRandomLine(i) });
            }

            dialog.Body.AddChild(scrollPane);
            dialog.AddButton("ok", "OK", null, null, null);
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