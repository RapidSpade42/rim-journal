using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace RapidSpade.RimJournal
{
    [StaticConstructorOnStartup]
    public class MainTabWindow_RimJournal : Page
    {

        // Private variables
        private string journalTitle = "";
        private string journalText = "";
        private List<string> savedEntries = new List<string>();
        private float journalTabWidth = 715f;
        private float journalTabHeight = 875f;
        private string selectedEntry = "";

        private enum Tab
        {
            Journal,
            Settings
        }

        private Tab currentTab = Tab.Journal;

        // Sets default width, height
        public override Vector2 InitialSize => new Vector2(journalTabWidth, journalTabHeight);

        public override string PageTitle => "RimJournal";

        // Draws the close button at the top right corner of the window
        private void DrawCloseButton(Rect rect)
        {
            const float buttonSize = 24f;
            Rect closeButtonRect = new Rect(rect.xMax - buttonSize, rect.y, buttonSize, buttonSize);
            if (Widgets.ButtonImage(closeButtonRect, TexButton.CloseXSmall))
            {
                Close();
            }
        }

        //Renders page based on selection
        public override void DoWindowContents(Rect rect)
        {
            Rect tabsRect = rect;
            tabsRect.yMin += 45f;
            DrawTabs(tabsRect);
            Rect tabContentsRect = rect;
            tabContentsRect.yMin += 45f;

            if (currentTab == Tab.Journal)
            {
                LoadSavedEntries();
            }

            switch (currentTab)
            {
                case Tab.Journal:
                    DrawJournalTab(tabContentsRect);
                    break;
                case Tab.Settings:
                    DrawSettingsTab(tabContentsRect);
                    break;
            }

            DrawCloseButton(rect);
        }

        // Draws tabs at top of page
        private void DrawTabs(Rect rect)
        {
            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("Journal", () => currentTab = Tab.Journal, currentTab == Tab.Journal),
                new TabRecord("Settings", () => currentTab = Tab.Settings, currentTab == Tab.Settings)
            };
            float tabHeight = 40f;
            Rect tabsRect = new Rect(rect.x, rect.y, rect.width, tabHeight);
            TabDrawer.DrawTabs(tabsRect, tabs, 200f);
        }

        // Draws journal tab and adds fields, labels, & buttons
        private void DrawJournalTab(Rect rect)
        {
            float buttonWidth = 100f;
            float buttonHeight = 30f;
            float buttonSpacing = 10f;
            float totalButtonWidth = 4 * buttonWidth + 3 * buttonSpacing;
            float buttonY = rect.height - buttonHeight - 10f;

            // Create labels & text fields
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 80f, rect.width, 30f), "Entry Title");
            Text.Anchor = TextAnchor.UpperLeft;
            Rect titleInputRect = new Rect((rect.width - rect.width / 2f) / 2f, 110f, rect.width / 2f, 30f);
            journalTitle = Widgets.TextField(titleInputRect, journalTitle);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0f, 150f, rect.width, 30f), "Entry Body");
            Text.Anchor = TextAnchor.UpperLeft;
            Rect textAreaRect = new Rect(0f, 180f, rect.width, rect.height - 250f);
            journalText = Widgets.TextArea(textAreaRect, journalText);

            // Save button
            Rect saveButtonRect = new Rect((rect.width - totalButtonWidth) / 2f, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveButtonRect, "Save"))
            {
                SaveJournalEntry();
            }

            // New button (just clears the text fields)
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
                if (savedEntries.Any())
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (string entry in savedEntries)
                    {
                        options.Add(new FloatMenuOption(entry, () =>
                        {
                            selectedEntry = entry;
                            LoadJournalEntry(selectedEntry);
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

        // Draws settings tab and adds options
        private void DrawSettingsTab(Rect rect)
        {
            float buttonHeight = 30f;
            float verticalPadding = 5f;

            float buttonY = buttonHeight + (verticalPadding * 6);

            // Only need this setting for now, may add more later
            Rect openExplorerButtonRect = new Rect(0f, buttonY, rect.width, buttonHeight);
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
        }

        // Saves current journal entry to export folder
        private void SaveJournalEntry()
        {
            // Checks to see if title/file exists
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

            // Creates directory if non-existant, saves entry
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                using (StreamWriter writer = File.CreateText(filePath))
                {
                    writer.Write(journalText);
                }

                Messages.Message("Journal entry saved to: " + filePath, MessageTypeDefOf.TaskCompletion, false);

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

        // Loads existing entries for display
        private void LoadSavedEntries()
        {
            savedEntries.Clear();

            string folderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "RimJournal");
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

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

        // Loads selected journal entry
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

        // Confirmation dialog before deletion
        private void ConfirmDeleteEntry()
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "Are you sure you want to delete this entry?",
                delegate { DeleteJournalEntry(); },
                true
            ));
        }

        // Deletes selected entry
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

        // Checks for files with same title
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
    public class MainButtonWorker_RimJournal : MainButtonWorker
    {
        public override void Activate()
        {
            Find.WindowStack.Add(new MainTabWindow_RimJournal());
        }
    }
}
