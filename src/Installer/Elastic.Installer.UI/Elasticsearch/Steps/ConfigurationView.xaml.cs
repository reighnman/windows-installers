﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	/// <summary>
	/// Interaction logic for ConfigurationText.xaml
	/// </summary>
	public partial class ConfigurationView : StepControl<ConfigurationModel, ConfigurationView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ConfigurationModel), typeof(ConfigurationView), new PropertyMetadata(null, ViewModelPassed));

		public override ConfigurationModel ViewModel
		{
			get => (ConfigurationModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		private readonly Brush _defaultBrush;
		public ConfigurationView()
		{
			InitializeComponent();
			this._defaultBrush = this.NodeNameTextBox.BorderBrush;
		}

		protected override void InitializeBindings()
		{
			this.ViewModel.AddSeedHostUserInterfaceTask = () =>
			{
				var metroWindow = (Application.Current.MainWindow as MetroWindow);
				return metroWindow.ShowInputAsync(
					ViewResources.ConfigurationView_AddSeedHost_Title,
					ViewResources.ConfigurationView_AddSeedHost_Message);
			};

			this.AddSeedHostButton.Command = this.ViewModel.AddSeedHost;
			this.RemoveSeedHostButton.Command = this.ViewModel.RemoveSeedHost;

			this.HttpPortTextBox.Minimum = ConfigurationModel.HttpPortMinimum;
			this.HttpPortTextBox.Maximum = ConfigurationModel.PortMaximum;
			this.TransportPortTextBox.Minimum = ConfigurationModel.TransportPortMinimum;
			this.TransportPortTextBox.Maximum = ConfigurationModel.PortMaximum;

			this.Bind(ViewModel, vm => vm.InitialMaster, v => v.InitialMasterNodeCheckBox.IsChecked);
			this.OneWayBind(ViewModel, vm => vm.SeedHosts, v => v.SeedHostsListBox.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedSeedHost, v => v.SeedHostsListBox.SelectedItem);
			this.Bind(ViewModel, vm => vm.ClusterName, v => v.ClusterNameTextBox.Text);
			this.Bind(ViewModel, vm => vm.NodeName, v => v.NodeNameTextBox.Text);
			this.Bind(ViewModel, vm => vm.NetworkHost, v => v.NetworkHostTextBox.Text);
			this.Bind(ViewModel, vm => vm.MasterNode, v => v.MasterNodeCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.DataNode, v => v.DataNodeCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.IngestNode, v => v.IngestNodeCheckBox.IsChecked);

			this.OneWayBind(ViewModel, vm => vm.SelectedMemory, v => v.MbLabel.Content, m => $"{FormatMb(m)}/{FormatMb(this.ViewModel.TotalPhysicalMemory)}");
			this.OneWayBind(ViewModel, vm => vm.MaxSelectedMemory, v => v.MemorySlider.Maximum);
			this.OneWayBind(ViewModel, vm => vm.MinSelectedMemory, v => v.MemorySlider.Minimum);
			this.Bind(ViewModel, vm => vm.SelectedMemory, v => v.MemorySlider.Value);
			this.Bind(ViewModel, vm => vm.LockMemory, v => v.LockMemoryCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.HttpPort, v => v.HttpPortTextBox.Value, null, new NullableIntToNullableDoubleConverter(), new NullableDoubleToNullableIntConverter());
			this.Bind(ViewModel, vm => vm.TransportPort, v => v.TransportPortTextBox.Value, null, new NullableIntToNullableDoubleConverter(), new NullableDoubleToNullableIntConverter());

			this.WhenAnyValue(v => v.ViewModel.MasterNode, v => v.ViewModel.UpgradingFrom6OrNewInstallation).Subscribe(
				t =>
				{
					bool masterEligable = t.Item1, newOrUpgradeFrom6 = t.Item2;
					this.InitialMasterNodeCheckBox.Visibility = (masterEligable && newOrUpgradeFrom6)
						? Visibility.Visible
						: Visibility.Collapsed;
				});

		}


		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
			var b = isValid ? this._defaultBrush : new SolidColorBrush(Color.FromRgb(233,73,152));
			this.NodeNameTextBox.BorderBrush = _defaultBrush;
			this.ClusterNameTextBox.BorderBrush = _defaultBrush;
			if (isValid) return;
			foreach (var e in this.ViewModel.ValidationFailures)
			{
				switch (e.PropertyName)
				{
					case nameof(ViewModel.ClusterName):
						this.ClusterNameTextBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.NodeName):
						this.NodeNameTextBox.BorderBrush = b;
						continue;
				}
			}
		}

		private static string FormatMb(ulong megabytes)
		{
			const int scale = 1024;
			var orders = new [] { "GB", "MB" };
			var max = (ulong)Math.Pow(scale, orders.Length - 1);

			foreach (var order in orders)
			{
				if (megabytes > max)
					return $"{decimal.Divide(megabytes, max):##.##} {order}";

				max /= scale;
			}
			return "0Mb";
		}
	}
}
