﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using SSCommonLib;
using SuperSocket.ClientEngine;

namespace Client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        static EasyClient<MyPackageInfo> client = null;
        static System.Timers.Timer timer = null;
        private int port = 8089;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            client = new EasyClient<MyPackageInfo>();
            client.Initialize(new MyReceiveFilter());
            client.Connected += OnClientConnected;
            client.NewPackageReceived += Client_NewPackageReceived;
            client.Error += OnClientError;
            client.Closed += OnClientClosed;

            //每2s发送一次心跳或尝试一次重连
            timer = new System.Timers.Timer(2000);
            timer.Elapsed += new System.Timers.ElapsedEventHandler((s, x) =>
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    //心跳包
                    if (client.IsConnected && cbSendHeart.IsChecked == true)
                    {
                        var heartMsg = CommandBuilder.BuildHeartCmd();
                        client.Send(heartMsg);
                    }
                    //断线重连
                    else if (!client.IsConnected)
                    {
                        client.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
                    }
                }));

            });
            timer.Enabled = true;
            timer.Start();
        }
        private void OnClientConnected(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                txbReceive.AppendText("已连接" + '\n');
            }));
        }
        private void OnClientClosed(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                txbReceive.AppendText("已断开" + '\n');
            }));
        }
        private void OnClientError(object sender, ErrorEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                txbReceive.AppendText($"错误：{e.Exception.Message}" + '\n');
            }));
        }
        private void Client_NewPackageReceived(object sender, PackageEventArgs<MyPackageInfo> e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!e.Package.IsHeart)
                    txbReceive.AppendText($"收到消息：{e.Package.Body}" + '\n');
                else if (cbIgnoreHeart.IsChecked == false)
                {
                    txbReceive.AppendText($"收到心跳反馈：{e.Package.Body}" + '\n');
                }
            }));
        }
        private void btnSendClear_Click(object sender, RoutedEventArgs e)
        {
            txbSend.Clear();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            var msg = CommandBuilder.BuildMsgCmd(txbSend.Text);
            if (client != null && client.IsConnected)
            {
                client.Send(msg);
            }
        }

        private void btnRecClear_Click(object sender, RoutedEventArgs e)
        {
            txbReceive.Text = String.Empty;
        }

        private void TxbReceive_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            txbReceive.ScrollToEnd();
        }
    }
}
