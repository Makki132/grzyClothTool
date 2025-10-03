using CodeWalker;
using CodeWalker.GameFiles;
using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Input;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System.Threading.Tasks;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Project.xaml
    /// </summary>
    public partial class ProjectWindow : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Addon _addon;
        public Addon Addon
        {
            get { return _addon; }
            set
            {
                if (_addon != value)
                {
                    _addon = value;
                    OnPropertyChanged();
                }
            }
        }

        public ProjectWindow()
        {
            InitializeComponent();

            if(DesignerProperties.GetIsInDesignMode(this))
            {
                Addon = new Addon("design");
                DataContext = this;
                return;
            }

            DataContext = MainWindow.AddonManager;
        }

        private async void Add_DrawableFile(object sender, RoutedEventArgs e)
        {
            var btn = sender as CustomButton;
            var sexBtn = btn.Label.ToString().Equals("male", StringComparison.CurrentCultureIgnoreCase) ? Enums.SexType.male : Enums.SexType.female;
            e.Handled = true;

            OpenFileDialog files = new()
            {
                Title = $"Select drawable files ({btn.Label})",
                Filter = "Drawable files (*.ydd)|*.ydd",
                Multiselect = true
            };

            if (files.ShowDialog() == true)
            {
                ProgressHelper.Start();

                await MainWindow.AddonManager.AddDrawables(files.FileNames, sexBtn);

                ProgressHelper.Stop("Added drawables in {0}", true);
                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private async void Add_DrawableFolder(object sender, RoutedEventArgs e)
        {
            var btn = sender as CustomButton;
            var sexBtn = btn.Tag.ToString().Equals("male", StringComparison.CurrentCultureIgnoreCase) ? Enums.SexType.male : Enums.SexType.female;
            e.Handled = true;

            OpenFolderDialog folder = new()
            {
                Title = $"Select a folder containing drawable files ({btn.Tag})",
                Multiselect = true
            };

            if (folder.ShowDialog() == true)
            {
                ProgressHelper.Start();

                foreach (var fldr in folder.FolderNames)
                {
                    var files = Directory.GetFiles(fldr, "*.ydd", SearchOption.AllDirectories).OrderBy(f => Path.GetFileName(f)).ToArray();
                    await MainWindow.AddonManager.AddDrawables(files, sexBtn);
                }

                ProgressHelper.Stop("Added drawables in {0}", true);
                SaveHelper.SetUnsavedChanges(true);
            }
        }

        public void SelectedDrawable_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete || Addon.SelectedDrawables.Count == 0)
            {
                return;
            }

            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Shift:
                    // Shift+Delete was pressed, delete the drawable instantly
                    MainWindow.AddonManager.DeleteDrawables([.. Addon.SelectedDrawables]);
                    break;
                case ModifierKeys.Control:
                    // Ctrl+Delete was pressed, replace the drawable instantly
                    ReplaceDrawables([.. Addon.SelectedDrawables]);
                    break;
                default:
                    // Only Delete was pressed, show the message box
                    Delete_SelectedDrawable(sender, new RoutedEventArgs());
                    break;
            }
        }

        private void Delete_SelectedDrawable(object sender, RoutedEventArgs e)
        {
            var count = Addon.SelectedDrawables.Count;

            if (count == 0)
            {
                CustomMessageBox.Show("No drawable(s) selected", "Delete drawable", CustomMessageBox.CustomMessageBoxButtons.OKOnly);
                return;
            }

            var message = count == 1
                ? $"Are you sure you want to delete this drawable? ({Addon.SelectedDrawable.Name})"
                : $"Are you sure you want to delete these {count} selected drawables?";

            message += "\nThis will CHANGE NUMBERS of everything after this drawable!\n\nDo you want to replace with reserved slot instead?";

            var result = CustomMessageBox.Show(message, "Delete drawable", CustomMessageBox.CustomMessageBoxButtons.DeleteReplaceCancel);
            if (result == CustomMessageBox.CustomMessageBoxResult.Delete)
            {
                MainWindow.AddonManager.DeleteDrawables([.. Addon.SelectedDrawables]);
            }
            else if (result == CustomMessageBox.CustomMessageBoxResult.Replace)
            {
                ReplaceDrawables([.. Addon.SelectedDrawables]);
            }
        }

        private void ReplaceDrawables(List<GDrawable> drawables)
        {
            foreach(var drawable in drawables)
            {
                var reserved = new GDrawableReserved(drawable.Sex, drawable.IsProp, drawable.TypeNumeric, drawable.Number);

                //replace drawable with reserved in the same place
                Addon.Drawables[Addon.Drawables.IndexOf(drawable)] = reserved;
            }
            SaveHelper.SetUnsavedChanges(true);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var addon = e.AddedItems[0] as Addon;
                int index = int.Parse(addon.Name.ToString().Split(' ')[1]) - 1;

                // as we are modyfing the collection, we need to use try-catch
                try
                {
                    Addon = MainWindow.AddonManager.Addons.ElementAt(index);
                    MainWindow.AddonManager.SelectedAddon = Addon;

                    foreach (var menuItem in MainWindow.AddonManager.MoveMenuItems)
                    {
                        menuItem.IsEnabled = menuItem.Header != addon.Name;
                    }
                } catch (Exception)  { }
            }
        }

        private void BuildResource_Btn(object sender, RoutedEventArgs e)
        {
            BuildWindow buildWindow = new()
            {
                Owner = Window.GetWindow(this)
            };
            buildWindow.ShowDialog();
        }

        private void Preview_Btn(object sender, RoutedEventArgs e)
        {
            if (CWHelper.CWForm == null || CWHelper.CWForm.IsDisposed)
            {
                CWHelper.CWForm = new CustomPedsForm();
                CWHelper.CWForm.FormClosed += CWForm_FormClosed;
            }

            if (Addon.SelectedDrawable == null)
            {
                CWHelper.CWForm.Show();
                MainWindow.AddonManager.IsPreviewEnabled = true;
                return;
            }

            var ydd = CWHelper.CreateYddFile(Addon.SelectedDrawable);
            CWHelper.CWForm.LoadedDrawables.Add(Addon.SelectedDrawable.Name, ydd.Drawables.First());

            if (Addon.SelectedTexture != null)
            {
                var ytd = CWHelper.CreateYtdFile(Addon.SelectedTexture, Addon.SelectedTexture.DisplayName);
                CWHelper.CWForm.LoadedTextures.Add(ydd.Drawables.First(), ytd.TextureDict);
            }

            CWHelper.SetPedModel(Addon.SelectedDrawable.Sex);

            CWHelper.CWForm.Show();
            MainWindow.AddonManager.IsPreviewEnabled = true;
        }

        private void CWForm_FormClosed(object sender, FormClosedEventArgs e)
        {
                MainWindow.AddonManager.IsPreviewEnabled = false;
        }

        private void SelectedDrawable_Changed(object sender, EventArgs e)
        {
            if (e is not SelectionChangedEventArgs args) return;
            args.Handled = true;

            foreach (GDrawable drawable in args.RemovedItems)
            {
                Addon.SelectedDrawables.Remove(drawable);
            }

            foreach (GDrawable drawable in args.AddedItems)
            {
                Addon.SelectedDrawables.Add(drawable);
                drawable.IsNew = false;
            }

            // Handle the case when a single item is selected
            if (Addon.SelectedDrawables.Count == 1)
            {
                Addon.SelectedDrawable = Addon.SelectedDrawables.First();
                if (Addon.SelectedDrawable.Textures.Count > 0)
                {
                    Addon.SelectedTexture = Addon.SelectedDrawable.Textures.First();
                    SelDrawable.SelectedIndex = 0;
                    SelDrawable.SelectedTextures = [Addon.SelectedTexture];
                }
            }
            else
            {
                Addon.SelectedDrawable = null;
                Addon.SelectedTexture = null;
            }

            if (!MainWindow.AddonManager.IsPreviewEnabled || (Addon.SelectedDrawable == null && Addon.SelectedDrawables.Count == 0)) return;
            CWHelper.SendDrawableUpdateToPreview(e);
        }

        private void SelectedDrawable_Updated(object sender, DrawableUpdatedArgs e)
        {
            if (!Addon.TriggerSelectedDrawableUpdatedEvent ||
                !MainWindow.AddonManager.IsPreviewEnabled ||
                (Addon.SelectedDrawable is null && Addon.SelectedDrawables.Count == 0) ||
                Addon.SelectedDrawables.All(d => d.Textures.Count == 0))
            {
                return;
            }

            CWHelper.SendDrawableUpdateToPreview(e);
        }

        private void SelectedDrawable_TextureChanged(object sender, EventArgs e)
        {
            if (e is not SelectionChangedEventArgs args || args.AddedItems.Count == 0)
            {
                Addon.SelectedTexture = null;
                return;
            }

            args.Handled = true;
            Addon.SelectedTexture = (GTexture)args.AddedItems[0];

            if (!MainWindow.AddonManager.IsPreviewEnabled || Addon.SelectedDrawable == null) return;

            try {
                // First ensure the drawable is loaded
                if (!CWHelper.CWForm.LoadedDrawables.ContainsKey(Addon.SelectedDrawable.Name)) {
                    var ydd = CWHelper.CreateYddFile(Addon.SelectedDrawable);
                    if (ydd != null && ydd.Drawables.Any()) {
                        CWHelper.CWForm.LoadedDrawables[Addon.SelectedDrawable.Name] = ydd.Drawables.First();
                    } else {
                        LogHelper.Log($"Could not create YDD file for {Addon.SelectedDrawable.Name}", Views.LogType.Error);
                        return;
                    }
                }

                var ytd = CWHelper.CreateYtdFile(Addon.SelectedTexture, Addon.SelectedTexture.DisplayName);
                var cwydd = CWHelper.CWForm.LoadedDrawables[Addon.SelectedDrawable.Name];
                CWHelper.CWForm.LoadedTextures[cwydd] = ytd.TextureDict;
                CWHelper.CWForm.Refresh();
            } catch (Exception ex) {
                LogHelper.Log($"Error updating texture preview: {ex.Message}", Views.LogType.Error);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Search functionality
        private void SearchBox_TextChanged(object sender, RoutedEventArgs e)
        {
            var searchBox = sender as SearchBox;
            var searchText = searchBox?.SearchText ?? string.Empty;

            if (MainWindow.AddonManager?.SelectedAddon?.Drawables == null)
                return;

            // Find the DrawableList control in the current tab and apply the filter
            var tabControl = FindName("drawableSearchBox");
            if (tabControl != null)
            {
                var activeTab = GetSelectedTabContent();
                if (activeTab != null)
                {
                    var drawableList = FindVisualChild<DrawableList>(activeTab);
                    if (drawableList != null)
                    {
                        drawableList.SearchText = searchText;
                    }
                }
            }
        }

        // Helper method to get the content of the selected tab
        private ContentPresenter GetSelectedTabContent()
        {
            var tabControl = FindVisualChild<System.Windows.Controls.TabControl>(this);
            if (tabControl != null && tabControl.SelectedIndex >= 0)
            {
                return tabControl.Template.FindName("PART_SelectedContentHost", tabControl) as ContentPresenter;
            }
            return null;
        }

        // Helper method to find a child of a specific type in the visual tree
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        // New button handlers
        private async void ImportPackage_Click(object sender, RoutedEventArgs e)
        {
            await MainWindow.Instance.ImportProjectAsync(false);
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            // Add with automatic gender detection for unisex clothing
            OpenFolderDialog folder = new()
            {
                Title = "Select a folder containing drawable files (mixed male/female supported)",
                Multiselect = false
            };

            if (folder.ShowDialog() == true)
            {
                ProgressHelper.Start("Started loading drawables with gender detection");

                var files = Directory.GetFiles(folder.FolderName, "*.ydd", SearchOption.AllDirectories)
                    .OrderBy(f => Path.GetFileName(f))
                    .ToArray();

                if (files.Length == 0)
                {
                    LogHelper.Log("No .ydd files found in the selected folder", LogType.Warning);
                    ProgressHelper.Stop("No files found", false);
                    return;
                }

                int maleCount = 0;
                int femaleCount = 0;

                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                    
                    // Detect gender using priority-based detection:
                    // 1. YDD file contents AND matching YTD files (most reliable)
                    // 2. Filename patterns
                    // 3. Parent folder name
                    // 4. Default to male
                    Enums.SexType detectedGender;
                    
                    // Find matching texture files for cross-file detection
                    var (isProp, drawableType, isBodyPart) = Helpers.FileHelper.ResolveDrawableType(file);
                    if (drawableType == -1) continue;
                    var drawableName = EnumHelper.GetName(drawableType, isProp, isBodyPart);
                    var matchingTextures = Helpers.FileHelper.FindMatchingTextures(file, drawableName, isProp);
                    
                    // Try YDD file contents + matching YTD files first (most reliable)
                    var genderFromYdd = await Helpers.GenderDetectionHelper.DetectGenderFromYddAsync(file, matchingTextures);
                    if (genderFromYdd.HasValue)
                    {
                        detectedGender = genderFromYdd.Value;
                        if (detectedGender == Enums.SexType.male)
                            maleCount++;
                        else
                            femaleCount++;
                    }
                    // Try filename patterns
                    else
                    {
                        var genderFromFilename = Helpers.GenderDetectionHelper.DetectGenderFromFilename(file);
                        if (genderFromFilename.HasValue)
                        {
                            detectedGender = genderFromFilename.Value;
                            if (detectedGender == Enums.SexType.male)
                                maleCount++;
                            else
                                femaleCount++;
                        }
                        else
                        {
                            // Try parent folder name
                            var parentFolder = Path.GetFileName(Path.GetDirectoryName(file))?.ToLower();
                            if (parentFolder?.Contains("female") == true || parentFolder?.Contains("_f") == true)
                            {
                                detectedGender = Enums.SexType.female;
                                femaleCount++;
                            }
                            else if (parentFolder?.Contains("male") == true || parentFolder?.Contains("_m") == true)
                            {
                                detectedGender = Enums.SexType.male;
                                maleCount++;
                            }
                            else
                            {
                                // Prompt user with full import configuration dialog
                                LogHelper.Log($"Could not fully auto-detect properties for '{fileName}', prompting user", LogType.Info);
                                
                                var dialog = new Controls.Dialogs.ImportConfigDialog(file)
                                {
                                    Owner = Window.GetWindow(this)
                                };
                                
                                if (dialog.ShowDialog() == true && !dialog.Config.WasCancelled)
                                {
                                    detectedGender = dialog.Config.Gender;
                                    if (detectedGender == Enums.SexType.male)
                                        maleCount++;
                                    else
                                        femaleCount++;
                                }
                                else // User cancelled
                                {
                                    LogHelper.Log($"Import cancelled by user at file '{fileName}'", LogType.Warning);
                                    ProgressHelper.Stop("Import cancelled by user", false);
                                    return;
                                }
                            }
                        }
                    }

                    await MainWindow.AddonManager.AddDrawables([file], detectedGender);
                }

                var message = $"Added {files.Length} drawable(s) ({maleCount} male, {femaleCount} female) with automatic gender detection in {{0}}";
                ProgressHelper.Stop(message, true);
                SaveHelper.SetUnsavedChanges(true);
            }
        }
    }
}
