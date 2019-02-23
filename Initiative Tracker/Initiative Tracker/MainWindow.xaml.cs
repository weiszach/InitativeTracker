using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Initiative_Tracker
{
    //TODO
    //add saved combats and name them to allow for planning of combats before hand
    //add ability to not accounce a unit (in case it's hidden from sight but still active)
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private static List<Unit> allUnits = new List<Unit>();
        private static Unit currentUnit = new Unit();
        private static SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        private static int round = 1;
        private static System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private static int elapsedDuration = 0;
        private static int allowableDuration = 0;
        public MainWindow()
        {
            InitializeComponent();

            try
            { 
                if(!System.IO.Directory.Exists(Environment.CurrentDirectory + "\\Sessions"))
                {
                    System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + "\\Sessions");
                }

                grdMain.ShowGridLines = false;
                
                synthesizer.Volume = 100;
                synthesizer.Rate = -2;
                btnStartNext.IsEnabled = false;
                btnPauseTimer.IsEnabled = false;
                chkTimer.IsEnabled = false;

                allUnits = (from c in allUnits
                            where c.hp > 0
                            orderby c.initative descending, c.initBonus descending, c.name ascending
                            select c).ToList();

                dgTracker.ItemsSource = allUnits;
                dgTracker.RowEditEnding += DgTracker_RowEditEnding;
                dgTracker.LoadingRow += dgTracker_LoadingRow;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region datagrid events
        bool eventHandled = false;
        private void dg_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                int index = dgTracker.CurrentColumn.DisplayIndex;
                if (index == dgTracker.Columns.Count - 1 && !eventHandled)
                {
                    eventHandled = true;
                    e.Handled = true;

                    var key = Key.Enter;
                    var target = Keyboard.FocusedElement;
                    var routedEvent = Keyboard.KeyDownEvent;

                    dgTracker.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(dgTracker), 0, key)
                    {
                        RoutedEvent = routedEvent
                    });

                    dgTracker.SelectedIndex = dgTracker.Items.Count - 1;
                   
                }
                else
                {
                    eventHandled = false;
                }
            }
        }

        private void DataGrid_CellGotFocus(object sender, RoutedEventArgs e)
        {
            // Lookup for the source to be DataGridCell
            if (e.OriginalSource.GetType() == typeof(DataGridCell))
            {
                // Starts the Edit on the row;
                DataGrid grd = (DataGrid)sender;
                grd.BeginEdit(e);

                Control control = GetFirstChildByType<Control>(e.OriginalSource as DataGridCell);
                if (control != null)
                {
                    control.Focus();
                }
            }
        }
        private void DgTracker_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            try
            { 
                int i = 0;

                if (this.dgTracker.SelectedItem != null)
                {
                    Unit selectedUnit = ((Unit)this.dgTracker.SelectedItem);
                    if(selectedUnit.name == null)
                    {
                        MessageBox.Show("You must specify a name");
                        e.Cancel = true;
                    }
                    if (selectedUnit.dmg > 0 && selectedUnit.name != null)
                    {
                        selectedUnit.hp = selectedUnit.hp - selectedUnit.dmg;
                        selectedUnit.dmg = 0;

                        if(selectedUnit.hp <= 0)
                        {
                            if((bool)chkTimer.IsChecked)
                            {
                                elapsedDuration = 0;
                            }
                            lblNotifications.Content = selectedUnit.name + " has died";
                            if ((bool)chkAudio.IsChecked)
                            {
                                synthesizer.SpeakAsync(selectedUnit.name + " has died");
                            }
                            allUnits = (from c in allUnits orderby c.initative descending, c.initBonus descending, c.name ascending select c).ToList();
                            //if we just got rid of the current unit we need to move
                            //on to the next unit to keep things moving along
                            if (currentUnit.name == selectedUnit.name &&
                                 currentUnit.initative == selectedUnit.initative)
                            {
                                foreach (Unit u in allUnits)
                                {
                                    if (u.name == currentUnit.name && u.initative == currentUnit.initative)
                                    {
                                        if (i + 1 == allUnits.Count())
                                        {
                                            currentUnit = allUnits[0];
                                            if ((bool)chkAudio.IsChecked)
                                            {
                                                synthesizer.SpeakAsync(currentUnit.name + "s turn");
                                            }
                                            lblCurrentPlayer.Content = currentUnit.name;
                                            break;
                                        }
                                        else
                                        {
                                            currentUnit = allUnits[i + 1];
                                            if ((bool)chkAudio.IsChecked)
                                            {
                                                synthesizer.SpeakAsync(currentUnit.name + "s turn");
                                            }
                                            lblCurrentPlayer.Content = currentUnit.name;
                                            break;
                                        }
                                    }

                                    i++;
                                }
                            }

                            dgTracker.CommitEdit();
                            dgTracker.CommitEdit();

                            allUnits = (from c in allUnits
                                        where c.hp > 0
                                        orderby c.initative descending, c.initBonus descending, c.name ascending
                                        select c).ToList();

                            dgTracker.ItemsSource = allUnits;

                            if (allUnits.Count > 0)
                            {
                                btnStartNext.IsEnabled = true;
                                chkTimer.IsEnabled = true;
                                btnPauseTimer.IsEnabled = true;
                            }
                        }
                        else
                        { 

                            dgTracker.CommitEdit();
                            dgTracker.CommitEdit();

                            allUnits = (from c in allUnits
                                        where c.hp > 0
                                        orderby c.initative descending, c.initBonus descending, c.name ascending
                                        select c).ToList();

                            dgTracker.ItemsSource = allUnits;

                            if(allUnits.Count > 0)
                            {
                                btnStartNext.IsEnabled = true;
                                chkTimer.IsEnabled = true;
                                btnPauseTimer.IsEnabled = true;
                            }
                        }


                    }
                    else
                    {
                        //see if its an add
                        if((from c in allUnits where c.name == selectedUnit.name && c.initative == selectedUnit.initative select c).Count() == 0)
                        {
                            allUnits.Add(selectedUnit);
                            
                            dgTracker.CommitEdit();
                            dgTracker.CommitEdit();

                            
                        }

                        if (!btnStartNext.IsEnabled)
                        {
                            btnStartNext.IsEnabled = true;
                            chkTimer.IsEnabled = true;
                            btnPauseTimer.IsEnabled = true;
                        }

                        allUnits = (from c in allUnits orderby c.initative descending, c.initBonus descending, c.name ascending select c).ToList();
                        dgTracker.ItemsSource = allUnits;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private static DataGridCell GetCell(DataGrid dataGrid, DataGridRow rowContainer, int column)
        {
            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }

            return null;
        }

        private static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void dgTracker_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            Unit newUnit = e.Row.Item as Unit;
            if (newUnit != null)
            {
                newUnit.hp = 1;
            }
        }

        #endregion

        #region button events

        private void btnResetRounds_Click(object sender, RoutedEventArgs e)
        {
            round = 1;
            lblRound.Content = "Round 1";
        }

        private void next_unit(object sender, RoutedEventArgs e)
        {
            try
            { 
                if(btnStartNext.Content.ToString() == "Next Round")
                {
                    round++;
                    lblRound.Content = "Round " + round;
                    synthesizer.SpeakAsync("Round " + round);
                }

                if(btnStartNext.Content.ToString() == "Begin Combat")
                {
                    lblRound.Content = "Round 1";
                }

                btnStartNext.Content = "Next Unit";
                int i = 0;
                allUnits = (from c in allUnits orderby c.initative descending, c.initBonus descending, c.name ascending select c).ToList();

                if(currentUnit.name == null)
                {
                    currentUnit = allUnits[0];
                }
                else
                {
                    foreach(Unit u in allUnits)
                    {
                        if(u.name == currentUnit.name && u.initative == currentUnit.initative)
                        {
                            if(i + 1 == allUnits.Count())
                            {
                                currentUnit = allUnits[0];
                                break;
                            }
                            else
                            {
                                if (i + 2 == allUnits.Count())
                                {
                                    btnStartNext.Content = "Next Round";
                                }
                                currentUnit = allUnits[i + 1];
                                break;
                            }
                        }

                        i++;
                    }
                }

                if ((bool)chkAudio.IsChecked)
                {
                    if (!currentUnit.hidden)
                    {
                        synthesizer.SpeakAsync(currentUnit.name + "s turn");
                    }
                }
                lblCurrentPlayer.Content = currentUnit.name;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnAnnounceCombat_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Open(new Uri(System.IO.Directory.GetCurrentDirectory() + "\\Resources\\battlestart.mp3"));
            mediaPlayer.Play();

            System.Threading.Thread.Sleep(4000);
        }

        private void btnPauseTimer_Click(object sender, RoutedEventArgs e)
        {
            if (btnPauseTimer.Content.ToString() == "Pause")
            {
                btnPauseTimer.Content = "Resume";
                dispatcherTimer.Stop();
            }
            else
            {
                btnPauseTimer.Content = "Pause";
                dispatcherTimer.Start();
            }
        }

        private void btnRollMonsters_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            
            foreach (Unit u in allUnits)
            {
                if(u.isMonster)
                {
                    int init = rnd.Next(1, 21);
                    u.initative = init;
                }
            }

            allUnits = (from c in allUnits orderby c.initative descending, c.initBonus descending, c.name ascending select c).ToList();

            dgTracker.ItemsSource = allUnits;
        }

        #endregion

        #region menu items

        private void mnLoadSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Multiselect = false;
                ofd.InitialDirectory = Environment.CurrentDirectory + "\\Sessions";
                ofd.ShowDialog();
                allUnits = new List<Unit>();
                if (ofd.FileName != "")
                {
                    round = 1;
                    lblRound.Content = "Round 1";
                    currentUnit = new Unit();
                    lblCurrentPlayer.Content = "";
                    lblNotifications.Content = "";
                    allUnits = new List<Unit>();
                    List<string> currentUnits = new List<string>();
                    List<string> fileLines = System.IO.File.ReadAllLines(ofd.FileName).ToList();
                    foreach (string line in fileLines)
                    {
                        currentUnits = line.Split(',').ToList();
                        allUnits.Add(new Unit()
                        {
                            name = currentUnits[0],
                            hp = int.Parse(currentUnits[1]),
                            initative = int.Parse(currentUnits[2]),
                            dmg = int.Parse(currentUnits[3]),
                            hidden = bool.Parse(currentUnits[4]),
                            isMonster = (currentUnits.Count == 6) ? bool.Parse(currentUnits[5]) : false
                        });
                    }

                    btnStartNext.IsEnabled = true;
                    chkTimer.IsEnabled = true;
                    btnPauseTimer.IsEnabled = true;
                    btnStartNext.Content = "Begin Combat";

                    allUnits = (from c in allUnits orderby c.initative descending, c.initBonus descending, c.name ascending select c).ToList();

                    dgTracker.ItemsSource = allUnits;
                    dgTracker.Items.SortDescriptions.Add(new SortDescription("initative", ListSortDirection.Descending));
                    dgTracker.Items.SortDescriptions.Add(new SortDescription("initBonus", ListSortDirection.Descending));
                    dgTracker.Items.SortDescriptions.Add(new SortDescription("name", ListSortDirection.Ascending));
                    dgTracker.RowEditEnding += DgTracker_RowEditEnding;

                    if (allUnits.Count == 0)
                    {
                        btnStartNext.IsEnabled = false;
                        chkTimer.IsEnabled = false;
                        btnPauseTimer.IsEnabled = false;
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + "|" + ex.StackTrace);
            }
        }

        private void mnSaveSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.InitialDirectory = Environment.CurrentDirectory + "\\Sessions";
                sfd.Filter = "Text (*.txt)|*.txt";
                sfd.ShowDialog();

                StringBuilder sb = new StringBuilder();

                foreach (Unit u in allUnits)
                {
                    sb.Append(u.name + "," + u.hp + "," + u.initative + "," + u.dmg + "," + u.hidden + "," + u.isMonster + Environment.NewLine);
                }

                if (sfd.FileName != "")
                {
                    System.IO.File.WriteAllText(sfd.FileName, sb.ToString());

                    MessageBox.Show("Session Saved");
                }
                else
                {
                    MessageBox.Show("A file name must be specified");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + "|" + ex.StackTrace);
            }
        }

        private void mnManageSessions_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Environment.CurrentDirectory + "\\Sessions");
        }

        private void Exit__Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void mnReplaceSound_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Environment.CurrentDirectory + "\\Resources");
        }

        #endregion

        #region helpers

        private T GetFirstChildByType<T>(DependencyObject prop) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(prop); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild((prop), i) as DependencyObject;
                if (child == null)
                    continue;

                T castedProp = child as T;
                if (castedProp != null)
                    return castedProp;

                castedProp = GetFirstChildByType<T>(child);

                if (castedProp != null)
                    return castedProp;
            }
            return null;
        }


        #endregion

        #region Timer

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            lblDuration.Content = allowableDuration - elapsedDuration;

            //move on to next unit
            if (elapsedDuration == allowableDuration)
            {
                try
                {
                    if (btnStartNext.Content.ToString() == "Next Round")
                    {
                        round++;
                        lblRound.Content = "Round " + round;
                        synthesizer.SpeakAsync("Round " + round);
                    }

                    if (btnStartNext.Content.ToString() == "Begin Combat")
                    {
                        lblRound.Content = "Round 1";
                    }

                    btnStartNext.Content = "Next Unit";
                    int i = 0;
                    allUnits = (from c in allUnits orderby c.initative descending, c.initBonus descending, c.name ascending select c).ToList();

                    if (currentUnit.name == null)
                    {
                        currentUnit = allUnits[0];
                    }
                    else
                    {
                        foreach (Unit u in allUnits)
                        {
                            if (u.name == currentUnit.name && u.initative == currentUnit.initative)
                            {
                                if (i + 1 == allUnits.Count())
                                {
                                    currentUnit = allUnits[0];
                                    break;
                                }
                                else
                                {
                                    if (i + 2 == allUnits.Count())
                                    {
                                        btnStartNext.Content = "Next Round";
                                    }
                                    currentUnit = allUnits[i + 1];
                                    break;
                                }
                            }

                            i++;
                        }
                    }

                    if ((bool)chkAudio.IsChecked)
                    {
                        if (!currentUnit.hidden)
                        {
                            synthesizer.SpeakAsync(currentUnit.name + "s turn");
                        }
                    }
                    lblCurrentPlayer.Content = currentUnit.name;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                elapsedDuration = 0;
            }
            else
            {
                elapsedDuration++;
            }
        }

        private void chkTimer_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            if((bool)chk.IsChecked)
            {
                
                allowableDuration = int.Parse(cbTimerDuration.Text);
                elapsedDuration = allowableDuration;
                dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += dispatcherTimer_Tick;
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
            }
            else
            {
                dispatcherTimer.Stop();
            }
            
        }

        #endregion

    }
}
