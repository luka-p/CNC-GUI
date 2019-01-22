﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using Microsoft.Win32;
using Clifton.Core.Pipes;
using System.Data.SqlClient;

namespace CNC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ClientPipe clientPipe;
        Thread pipeThread;
        SqlConnection con;
        SqlDataAdapter adapter;
        SqlCommand command;
        SqlDataReader reader;
        double maxX = 300;
        double maxY = 300;

        public MainWindow()
        {
            InitializeComponent();

            pipeThread = new Thread(PipeThread);
            pipeThread.IsBackground = true;
            pipeThread.Start();

            con = new SqlConnection();
            con.ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Luka\source\repos\CNC\CNC\Database1.mdf;Integrated Security=True";
            con.Open();

            /*
            con.ConnectionString = @"Data Source=.\SQLEXPRESS;
                          AttachDbFilename=C:\Users\Luka\source\repos\CNC\CNC\ErrorDatabase.mdf;
                          Integrated Security=True;
                          Connect Timeout=30;
                          User Instance=True";
            */
            adapter = new SqlDataAdapter();

        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            /*
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
            */
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            clientPipe.WriteString("Test");
        }

        private void PipeThread()
        {

            clientPipe = new ClientPipe(".", "pipe", p => p.StartStringReaderAsync());
            clientPipe.DataReceived += (sndr, args) =>
               Dispatcher.BeginInvoke(new Action(() =>
               {
                    ProcessInput(args.String);
               }));
            clientPipe.Connect();
        }

        public void StartMoveYPlus(object sender, MouseButtonEventArgs e)
        {
            clientPipe.WriteString("move start y+");
        }
        public void StopMoveYPlus(object sender, MouseButtonEventArgs e)
        {
            clientPipe.WriteString("move stop y+");
        }

        public void StartMoveXPlus(object sender, MouseButtonEventArgs e)
        {
            clientPipe.WriteString("move start x+");
        }
        public void StopMoveXPlus(object sender, MouseButtonEventArgs e)
        {
            clientPipe.WriteString("move stop x+");
        }

        public void StartMoveXMinus(object sender, MouseButtonEventArgs e)
        {
            clientPipe.WriteString("move start x-");
        }
        public void StopMoveXMinus(object sender, MouseButtonEventArgs e)
        {
            clientPipe.WriteString("move stop x-");
        }

        public void StartMoveYMinus(object sender, MouseButtonEventArgs e)
        {
            clientPipe.WriteString("move start y-");
        }
        public void StopMoveYMinus(object sender, MouseButtonEventArgs e)
        {
            clientPipe.WriteString("move stop y-");
        }

        public void ProcessInput(string input)
        {
            string[] splitInput = input.Split(' ');
            switch (splitInput[0])
            {
                case "ERROR":
                    command = new SqlCommand("INSERT INTO Errors (Error_ID, Time) VALUES (@ID, @TIME);", con);
                    command.Parameters.AddWithValue("@ID", splitInput[1]);
                    command.Parameters.AddWithValue("@TIME", DateTime.Now);
                    command.ExecuteNonQuery();
                    break;
                case "POSITION":
                    double x = 0, y = 0 ;
                    double.TryParse(splitInput[1], out x);
                    double.TryParse(splitInput[2], out y);
                    UpdateVisualization(x, y);
                    posX.Text = String.Format("X: {0}", splitInput[1]);
                    posY.Text = String.Format("Y: {0}", splitInput[2]);
                    break;
            }

            
        }

        private void showErrorsButton_Click(object sender, RoutedEventArgs e)
        {
            String output = "";
            command = new SqlCommand("SELECT Error_ID, Time FROM Errors;", con);
            reader = command.ExecuteReader();
            while (reader.Read())
            {
                var id = reader.GetValue(0);
                DateTime time = reader.GetDateTime(1);
                output = output + id + " - " + time + "\n";
            }
            reader.Close();
            MessageBox.Show(output);

        }

        private void UpdateVisualization(double x, double y)
        {
            verticalLine.X1 = MapValue(0, maxX, 0, border.ActualWidth, x);
            verticalLine.X2 = MapValue(0, maxX, 0, border.ActualWidth, x);
            verticalLine.Y1 = 0;
            verticalLine.Y2 = border.ActualHeight;

            horizontalLine.Y1 = border.ActualHeight - MapValue(0, maxY, 0, border.ActualHeight, y);
            horizontalLine.Y2 = border.ActualHeight - MapValue(0, maxY, 0, border.ActualHeight, y);
            horizontalLine.X1 = 0;
            horizontalLine.X2 = border.ActualWidth;


        }

        public double MapValue(double a0, double a1, double b0, double b1, double a)
        {
            return b0 + (b1 - b0) * ((a - a0) / (a1 - a0));
        }
    }
}
