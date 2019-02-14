using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    //fix when you remove the selected unit it won't advance further (on delete check for next unit in the init stack
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private static List<Unit> allUnits = new List<Unit>();
        private static Unit currentUnit = new Unit();
        private static SpeechSynthesizer synthesizer = new SpeechSynthesizer();

        public MainWindow()
        {
            InitializeComponent();
            try
            { 
                synthesizer.Volume = 100;
                synthesizer.Rate = -2;

                if (!System.IO.File.Exists("lastsession.txt"))
                {
                    System.IO.File.Create("lastsession.txt");
                    btnStartNext.IsEnabled = false;
                }
                else
                {
                    List<string> currentUnit = new List<string>();
                    List<string> fileLines = System.IO.File.ReadAllLines("lastsession.txt").ToList();
                    foreach(string line in fileLines)
                    {
                        currentUnit = line.Split(',').ToList();
                        allUnits.Add(new Unit() { name = currentUnit[0], hp = int.Parse(currentUnit[1]), initative = int.Parse(currentUnit[2]) });
                    }

                    allUnits = (from c in allUnits orderby c.initative descending, c.name ascending select c).ToList();

                    if(allUnits.Count == 0)
                    {
                        btnStartNext.IsEnabled = false;
                    }
                }

                dgTracker.ItemsSource = allUnits;
                dgTracker.Items.SortDescriptions.Add(new SortDescription("initative", ListSortDirection.Descending));
                dgTracker.Items.SortDescriptions.Add(new SortDescription("name", ListSortDirection.Ascending));
                dgTracker.RowEditEnding += DgTracker_RowEditEnding;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                    if (selectedUnit.dmg > 0 && selectedUnit.name != null)
                    {
                        selectedUnit.hp = selectedUnit.hp - selectedUnit.dmg;
                        selectedUnit.dmg = 0;

                        if(selectedUnit.hp <= 0)
                        {
                            lblNotifications.Content = selectedUnit.name + " has died";
                            synthesizer.SpeakAsync(selectedUnit.name + " has died");
                            allUnits = (from c in allUnits orderby c.initative descending, c.name ascending select c).ToList();
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
                                            synthesizer.SpeakAsync(currentUnit.name + "s turn");
                                            lblCurrentPlayer.Content = currentUnit.name;
                                            break;
                                        }
                                        else
                                        {
                                            currentUnit = allUnits[i + 1];
                                            synthesizer.SpeakAsync(currentUnit.name + "s turn");
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
                                        orderby c.initative descending, c.name ascending
                                        select c).ToList();

                            dgTracker.ItemsSource = allUnits;

                        }
                        else
                        { 

                            dgTracker.CommitEdit();
                            dgTracker.CommitEdit();

                            allUnits = (from c in allUnits
                                        where c.hp > 0
                                        orderby c.initative descending, c.name ascending
                                        select c).ToList();

                            dgTracker.ItemsSource = allUnits;
                        }


                    }
                    else
                    {
                        //see if its an add
                        if((from c in allUnits where c.name == selectedUnit.name && c.initative == selectedUnit.initative select c).Count() == 0)
                        {
                            allUnits.Add(selectedUnit);
                            allUnits = (from c in allUnits orderby c.initative descending, c.name ascending select c).ToList();
                            dgTracker.CommitEdit();
                            dgTracker.CommitEdit();

                            dgTracker.ItemsSource = allUnits;
                        }

                        if (!btnStartNext.IsEnabled)
                        {
                            btnStartNext.IsEnabled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

         private void save_session(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                foreach (Unit u in allUnits)
                {
                    sb.Append(u.name + "," + u.hp + "," + u.initative + Environment.NewLine);
                }

                System.IO.File.WriteAllText("lastsession.txt", sb.ToString());

                MessageBox.Show("Session Saved");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void next_unit(object sender, RoutedEventArgs e)
        {
            try
            { 
                if(btnStartNext.Content.ToString() == "Begin Combat")
                {
                    mediaPlayer.Open(new Uri(System.IO.Directory.GetCurrentDirectory() + "\\Resources\\battlestart.mp3"));                
                    mediaPlayer.Play();

                    System.Threading.Thread.Sleep(4000);
                }
                btnStartNext.Content = "Next Unit";
                int i = 0;
                allUnits = (from c in allUnits orderby c.initative descending, c.name ascending select c).ToList();

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

                synthesizer.SpeakAsync(currentUnit.name + "s turn");
                lblCurrentPlayer.Content = currentUnit.name;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnStartNewCombat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                currentUnit = new Unit();
                lblCurrentPlayer.Content = "";
                lblNotifications.Content = "";
                allUnits = new List<Unit>();
                List<string> currentUnits = new List<string>();
                List<string> fileLines = System.IO.File.ReadAllLines("lastsession.txt").ToList();
                foreach (string line in fileLines)
                {
                    currentUnits = line.Split(',').ToList();
                    allUnits.Add(new Unit() { name = currentUnits[0], hp = int.Parse(currentUnits[1]), initative = int.Parse(currentUnits[2]) });
                }

                btnStartNext.Content = "Begin Combat";

                allUnits = (from c in allUnits orderby c.initative descending, c.name ascending select c).ToList();

                dgTracker.ItemsSource = allUnits;
                dgTracker.Items.SortDescriptions.Add(new SortDescription("initative", ListSortDirection.Descending));
                dgTracker.Items.SortDescriptions.Add(new SortDescription("name", ListSortDirection.Ascending));
                dgTracker.RowEditEnding += DgTracker_RowEditEnding;

                if (allUnits.Count == 0)
                {
                    btnStartNext.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
