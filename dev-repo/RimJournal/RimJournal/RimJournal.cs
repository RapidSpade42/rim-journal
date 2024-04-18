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
        private List<string> savedEntries = new List<string>();
        private float journalTabWidth = 700f; // Default width
        private float journalTabHeight = 875f; // Default heigh
        private string selectedEntry = "";

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
            if (currentTab == Tab.Journal)
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
            // Draw buttons
            float buttonWidth = 100f;
            float buttonHeight = 30f;
            float buttonSpacing = 10f;
            float totalButtonWidth = 4 * buttonWidth + 3 * buttonSpacing;
            float buttonY = rect.height - buttonHeight - 10f;

            // Label for title input
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 80f, rect.width, 30f), "Entry Title");
            Text.Anchor = TextAnchor.UpperLeft;

            // Text input area for title
            Rect titleInputRect = new Rect(0f, 110f, rect.width, 30f);
            journalTitle = Widgets.TextField(titleInputRect, journalTitle);

            // Label for body input
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 150f, rect.width, 30f), "Entry Body");
            Text.Anchor = TextAnchor.UpperLeft;

            // Text input area for journal body
            Rect textAreaRect = new Rect(0f, 180f, rect.width, rect.height - 250f); // Adjusted the height here
            journalText = Widgets.TextArea(textAreaRect, journalText);

            // Save button
            Rect saveButtonRect = new Rect((rect.width - totalButtonWidth) / 2f, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveButtonRect, "Save"))
            {
                SaveJournalEntry();
            }

            // New button
            Rect newButtonRect = new Rect(saveButtonRect.xMax + buttonSpacing, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(newButtonRect, "New"))
            {
                journalTitle = "";
                journalText = "";
            }

            // Load button
            Rect loadButtonRect = new Rect(newButtonRect.xMax + buttonSpacing, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(loadButtonRect, "Load"))
            {
                // Dropdown menu for saved entries
                if (savedEntries.Any())
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (string entry in savedEntries)
                    {
                        options.Add(new FloatMenuOption(entry, () =>
                        {
                            selectedEntry = entry;
                            LoadJournalEntry(selectedEntry); // Load the selected entry
                        }));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                else
                {
                    Messages.Message("No entries found.", MessageTypeDefOf.NeutralEvent, false);
                }
            }

            // Delete button
            Rect deleteButtonRect = new Rect(loadButtonRect.xMax + buttonSpacing, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(deleteButtonRect, "Delete") && !string.IsNullOrEmpty(journalTitle))
            {
                ConfirmDeleteEntry();
            }
        }


        private void DrawSettingsTab(Rect rect)
        {
            float labelHeight = 30f;
            float inputHeight = 30f;
            float buttonHeight = 30f;
            float verticalPadding = 5f;

            // Label for journal tab dimensions
            Rect dimensionsLabelRect = new Rect(0f, 80f, rect.width, labelHeight);
            Widgets.Label(dimensionsLabelRect, "Tab Dimensions:");

            // Label for reopening instructions
            Rect reopenLabelRect = new Rect(0f, dimensionsLabelRect.yMax + verticalPadding, rect.width, labelHeight);
            Widgets.Label(reopenLabelRect, "(Reopen for changes to take effect)");

            // Input field for width
            Rect widthInputRect = new Rect(0f, reopenLabelRect.yMax + verticalPadding, rect.width, inputHeight);
            Widgets.Label(widthInputRect.LeftHalf(), "Width:");
            journalTabWidth = Widgets.HorizontalSlider(widthInputRect.RightHalf(), journalTabWidth, 200f, 1500f);

            // Input field for height
            Rect heightInputRect = new Rect(0f, widthInputRect.yMax + verticalPadding, rect.width, inputHeight);
            Widgets.Label(heightInputRect.LeftHalf(), "Height:");
            journalTabHeight = Widgets.HorizontalSlider(heightInputRect.RightHalf(), journalTabHeight, 200f, 1000f);

            // Calculate positions for buttons
            float buttonY = rect.height - buttonHeight - verticalPadding;

            // Open Explorer button
            Rect openExplorerButtonRect = new Rect(0f, buttonY - buttonHeight - verticalPadding, rect.width, buttonHeight);
            if (Widgets.ButtonText(openExplorerButtonRect, "Open Export Directory"))
            {
                try
                {
                    string folderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "RimJournal");
                    if (Directory.Exists(folderPath))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", folderPath);
                    }
                    else
                    {
                        Messages.Message("Export directory not found.", MessageTypeDefOf.NeutralEvent, false);
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Error opening export directory: " + e.Message);
                }
            }

            // Reset Settings button
            Rect resetSettingsButtonRect = new Rect(0f, buttonY, rect.width, buttonHeight);
            if (Widgets.ButtonText(resetSettingsButtonRect, "Reset Settings"))
            {
                ResetSettings();
            }
        }

        private void ResetSettings()
        {
            journalTabWidth = 700f; // Default width
            journalTabHeight = 875f; // Default height

            Messages.Message("Settings reset to default values.", MessageTypeDefOf.TaskCompletion, false);
        }
        private void SaveJournalEntry()
        {
            if (string.IsNullOrEmpty(journalTitle))
            {
                Messages.Message("Cannot save without a title.", MessageTypeDefOf.RejectInput, false);
                return;
            }

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
                    
                    Messages.Message("Journal entry loaded: " + journalTitle, MessageTypeDefOf.TaskCompletion, false);
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
