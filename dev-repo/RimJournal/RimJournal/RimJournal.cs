using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace RapidSpade.RimJournal
{
    [StaticConstructorOnStartup]
    public class MainTabWindow_RimJournal : MainTabWindow
    {
        private string journalTitle = "";
        private string journalText = "";
        private Vector2 scrollPosition = Vector2.zero;
        private List<string> savedEntries = new List<string>();
        private float journalTabWidth = 1010f; // Default width
        private float journalTabHeight = 640f; // Default height

        private enum Tab
        {
            Journal,
            Settings
        }

        private Tab currentTab = Tab.Journal;

        public override Vector2 RequestedTabSize => new Vector2(journalTabWidth, journalTabHeight);

        public override void DoWindowContents(Rect rect)
        {
            Rect tabsRect = rect;
            tabsRect.yMin += 45f;

            // Draw tabs
            DrawTabs(tabsRect);

            // Adjust the rect for tab contents
            Rect tabContentsRect = rect;
            tabContentsRect.yMin += 45f; // Adjust for tab height

            // Load saved entries before rendering UI
            if (currentTab == Tab.Settings)
            {
                LoadSavedEntries();
            }

            // Draw tab contents based on current tab
            switch (currentTab)
            {
                case Tab.Journal:
                    DrawJournalTab(tabContentsRect);
                    break;
                case Tab.Settings:
                    DrawSettingsTab(tabContentsRect);
                    break;
            }
        }

        private void DrawTabs(Rect rect)
        {
            // Define tabs
            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("Journal", () => currentTab = Tab.Journal, currentTab == Tab.Journal),
                new TabRecord("Settings", () => currentTab = Tab.Settings, currentTab == Tab.Settings)
            };

            // Set the height of the tabsRect to make the tabs more visible
            float tabHeight = 40f; // Adjust the tab height as needed
            Rect tabsRect = new Rect(rect.x, rect.y, rect.width, tabHeight);

            // Draw tabs
            TabDrawer.DrawTabs(tabsRect, tabs, 200f);
        }

        private void DrawJournalTab(Rect rect)
        {
            // Label for title input
            Rect titleLabelRect = new Rect(0f, 80f, rect.width, 30f);
            Widgets.Label(titleLabelRect, "Entry title:");

            // Text input area for title
            Rect titleInputRect = new Rect(0f, 110f, rect.width, 30f);
            journalTitle = Widgets.TextField(titleInputRect, journalTitle);

            // Label for body input
            Rect bodyLabelRect = new Rect(0f, 150f, rect.width, 30f);
            Widgets.Label(bodyLabelRect, "Entry body:");

            // Text input area for journal body
            Rect textAreaRect = new Rect(0f, 180f, rect.width, rect.height - 250f); // Adjusted the height here
            journalText = Widgets.TextArea(textAreaRect, journalText);

            // Save button
            Rect saveButtonRect = new Rect((rect.width - 210f) / 2f, rect.height - 65f, 100f, 30f);
            if (Widgets.ButtonText(saveButtonRect, "Save"))
            {
                SaveJournalEntry();
            }

            // Delete button
            Rect deleteButtonRect = new Rect((rect.width + 10f) / 2f, rect.height - 65f, 100f, 30f);
            if (Widgets.ButtonText(deleteButtonRect, "Delete") && !string.IsNullOrEmpty(journalTitle))
            {
                ConfirmDeleteEntry();
            }
        }

        private void DrawSettingsTab(Rect rect)
        {
            // Label for journal tab dimensions
            Rect dimensionsLabelRect = new Rect(0f, 80f, rect.width, 30f);
            Widgets.Label(dimensionsLabelRect, "Tab Dimensions (reopen for changes to take effect):");

            // Input field for width
            Rect widthInputRect = new Rect(0f, 110f, rect.width, 30f);
            Widgets.Label(widthInputRect.LeftHalf(), "Width:");
            journalTabWidth = Widgets.HorizontalSlider(widthInputRect.RightHalf(), journalTabWidth, 200f, 1500f);

            // Input field for height
            Rect heightInputRect = new Rect(0f, 150f, rect.width, 30f);
            Widgets.Label(heightInputRect.LeftHalf(), "Height:");
            journalTabHeight = Widgets.HorizontalSlider(heightInputRect.RightHalf(), journalTabHeight, 200f, 1000f);

            // Label for saved entries
            Rect savedLabelRect = new Rect(0f, 190f, rect.width, 30f);
            Widgets.Label(savedLabelRect, "Saved Entries (click to load):");

            // Calculate the total height needed for all entries
            float entryHeight = 30f;
            float totalHeight = savedEntries.Count * entryHeight;

            // Use a ScrollView to enable scrolling if necessary
            Rect scrollRect = new Rect(0f, 220f, rect.width, rect.height - 220f);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalHeight);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            // Display saved entries
            for (int i = 0; i < savedEntries.Count; i++)
            {
                Rect entryRect = new Rect(0f, i * entryHeight, rect.width, entryHeight);
                if (Widgets.ButtonText(entryRect, savedEntries[i]))
                {
                    LoadJournalEntry(savedEntries[i]);
                }
            }

            Widgets.EndScrollView();
        }

        private void SaveJournalEntry()
        {
            string fileName;
            if (savedEntries.Contains(journalTitle))
            {
                fileName = journalTitle + "_" + FindSameTitleCount(journalTitle) + ".txt";
            }
            else
            {
                fileName = journalTitle + ".txt";
            }

            string folderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "RimJournal");
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                using (StreamWriter writer = File.CreateText(filePath))
                {
                    writer.Write(journalText);
                }

                // Show success message
                Messages.Message("Journal entry saved to: " + filePath, MessageTypeDefOf.TaskCompletion, false);

                // Reset text fields
                journalText = "";
                journalTitle = "";

                // Update saved entries list
                if (!savedEntries.Contains(fileName))
                {
                    savedEntries.Add(fileName);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error saving journal entry: " + e.Message);
            }
        }

        private void LoadSavedEntries()
        {
            // Clear the existing entries
            savedEntries.Clear();

            string folderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "RimJournal");
            try
            {
                // Create directory if it doesn't exist
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Read files in export folder
                string[] files = Directory.GetFiles(folderPath);
                foreach (string file in files)
                {
                    if (Path.GetExtension(file).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        savedEntries.Add(Path.GetFileName(file));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Error loading saved entries: " + e.Message);
            }
        }

        private void LoadJournalEntry(string fileName)
        {
            string filePath = Path.Combine(GenFilePaths.SaveDataFolderPath, "RimJournal", fileName);
            try
            {
                if (File.Exists(filePath))
                {
                    using (StreamReader reader = File.OpenText(filePath))
                    {
                        journalText = reader.ReadToEnd();
                    }
                    journalTitle = fileName.Replace(".txt", "");
                }
                else
                {
                    Log.Error("Journal entry file not found: " + filePath);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error loading journal entry: " + e.Message);
            }
        }

        private void ConfirmDeleteEntry()
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "Are you sure you want to delete this entry?",
                delegate { DeleteJournalEntry(); },
                true
            ));
        }

        private void DeleteJournalEntry()
        {
            string filePath = Path.Combine(GenFilePaths.SaveDataFolderPath, "RimJournal", journalTitle + ".txt");
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Messages.Message("Journal entry deleted: " + journalTitle, MessageTypeDefOf.NegativeEvent, false);
                    savedEntries.Remove(journalTitle + ".txt");
                    journalTitle = "";
                    journalText = "";
                }
                else
                {
                    Log.Error("Journal entry file not found: " + filePath);
                }
            }
            catch (Exception e)
            {
                Log.Error("Error deleting journal entry: " + e.Message);
            }
        }

        private int FindSameTitleCount(string title)
        {
            int count = 1;
            foreach (string entry in savedEntries)
            {
                if (entry.StartsWith(title + "_"))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
