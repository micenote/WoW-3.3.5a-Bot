﻿using AmeisenCore;
using AmeisenCore.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AmeisenBotGUI
{
    class DataItem
    {
        public string Text { get; set; }
        public Brush Background { get; set; }

        public DataItem(string text, Brush background)
        {
            Text = text;
            Background = background;
        }
    }

    /// <summary>
    /// Interaktionslogik für DebugUI.xaml
    /// </summary>
    public partial class DebugUI : Window
    {
        public DebugUI()
        {
            InitializeComponent();
        }

        private void DebugUI_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void buttonMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void debugUI_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer uiUpdateTimer = new DispatcherTimer();
            uiUpdateTimer.Tick += new EventHandler(UIUpdateTimer_Tick);
            uiUpdateTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            uiUpdateTimer.Start();
        }

        private void UIUpdateTimer_Tick(object sender, EventArgs e)
        {
            listboxObjects.Items.Clear();
            foreach (WoWObject obj in AmeisenManager.GetInstance().GetObjects())
            {
                if (obj.GetType() == typeof(WoWObject) && checkboxFilterWOWOBJECT.IsChecked == true)
                    listboxObjects.Items.Add(new DataItem(obj.ToString(), new SolidColorBrush((Color)Application.Current.Resources["WowobjectColor"])));
                else if (obj.GetType() == typeof(Unit) && checkboxFilterUNIT.IsChecked == true)
                    listboxObjects.Items.Add(new DataItem(obj.ToString(), new SolidColorBrush((Color)Application.Current.Resources["UnitColor"])));
                else if (obj.GetType() == typeof(Player) && checkboxFilterPLAYER.IsChecked == true)
                    listboxObjects.Items.Add(new DataItem(obj.ToString(), new SolidColorBrush((Color)Application.Current.Resources["PlayerColor"])));
                else if (obj.GetType() == typeof(Me) && checkboxFilterME.IsChecked == true)
                    listboxObjects.Items.Add(new DataItem(obj.ToString(), new SolidColorBrush((Color)Application.Current.Resources["MeColor"])));
            }
        }
    }
}