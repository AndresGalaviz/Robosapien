//------------------------------------------------------------------------------
// <copyright file="KinectSettings.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace Com.Enterprisecoding.RobosapienKinect.Viewers {
    /// <summary>
    ///     Interaction logic for KinectSettings.xaml
    /// </summary>
    internal partial class KinectSettings : UserControl {
        private readonly DispatcherTimer debounce = new DispatcherTimer {IsEnabled = false, Interval = TimeSpan.FromMilliseconds(200)};
        private bool backgroundUpdateInProgress;

        private int lastSetSensorAngle = int.MaxValue;
        private bool userUpdate = true;

        public KinectSettings(KinectDiagnosticViewer diagViewer) {
            DiagViewer = diagViewer;
            InitializeComponent();
            debounce.Tick += DebounceElapsed;
        }

        public KinectDiagnosticViewer DiagViewer { get; set; }

        public KinectSensor Kinect { get; set; }

        private static bool IsSkeletalViewerAvailable {
            get { return KinectSensor.KinectSensors.All(k => (!k.IsRunning || !k.SkeletonStream.IsEnabled)); }
        }

        internal void PopulateComboBoxesWithFormatChoices() {
            foreach (ColorImageFormat colorImageFormat in Enum.GetValues(typeof (ColorImageFormat))) {
                switch (colorImageFormat) {
                    case ColorImageFormat.Undefined:
                        break;
                    case ColorImageFormat.RawYuvResolution640x480Fps15:
                        // don't add RawYuv to combobox.
                        // That colorImageFormat works, but needs YUV->RGB conversion code which this sample doesn't have yet.
                        break;
                    default:
                        colorFormats.Items.Add(colorImageFormat);
                        break;
                }
            }

            foreach (DepthImageFormat depthImageFormat in Enum.GetValues(typeof (DepthImageFormat))) {
                switch (depthImageFormat) {
                    case DepthImageFormat.Undefined:
                        break;
                    default:
                        depthFormats.Items.Add(depthImageFormat);
                        break;
                }
            }

            foreach (TrackingMode trackingMode in Enum.GetValues(typeof (TrackingMode))) {
                trackingModes.Items.Add(trackingMode);
            }

            foreach (DepthRange depthRange in Enum.GetValues(typeof (DepthRange))) {
                depthRanges.Items.Add(depthRange);
            }

            depthRanges.SelectedIndex = 0;
        }

        internal void UpdateUiElevationAngleFromSensor() {
            if (Kinect != null) {
                userUpdate = false;

                // If it's never been set, retrieve the value.
                if (lastSetSensorAngle == int.MaxValue) {
                    lastSetSensorAngle = Kinect.ElevationAngle;
                }

                // Use the cache to prevent race conditions with the background thread which may 
                // be in the process of setting this value.
                ElevationAngle.Value = lastSetSensorAngle;
                userUpdate = true;
            }
        }

        private void ColorFormatsSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = sender as ComboBox;
            if (comboBox == null) {
                return;
            }

            if (Kinect != null && Kinect.Status == KinectStatus.Connected && comboBox.SelectedItem != null) {
                if (Kinect.ColorStream.IsEnabled) {
                    Kinect.ColorStream.Enable((ColorImageFormat) colorFormats.SelectedItem);
                }
            }
        }

        private void DepthFormatsSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = sender as ComboBox;
            if (comboBox == null) {
                return;
            }

            if (Kinect != null && Kinect.Status == KinectStatus.Connected && comboBox.SelectedItem != null) {
                if (Kinect.DepthStream.IsEnabled) {
                    Kinect.DepthStream.Enable((DepthImageFormat) depthFormats.SelectedItem);
                }
            }
        }

        private void SkeletonsChecked(object sender, RoutedEventArgs e) {
            var checkBox = sender as CheckBox;
            if (checkBox == null) {
                return;
            }

            if (Kinect != null && Kinect.Status == KinectStatus.Connected && checkBox.IsChecked.HasValue) {
                SetSkeletalTracking(checkBox.IsChecked.Value);
                EnableDepthStreamBasedOnDepthOrSkeletonEnabled(Kinect.DepthStream, depthFormats);
            }
        }

        private void SetSkeletalTracking(bool enable) {
            if (enable) {
                if (IsSkeletalViewerAvailable) {
                    Kinect.SkeletonStream.Enable();
                    trackingModes.IsEnabled = true;
                    DiagViewer.KinectSkeletonViewerOnColor.Visibility = Visibility.Visible;
                    DiagViewer.KinectSkeletonViewerOnDepth.Visibility = Visibility.Visible;
                    SkeletonStreamEnable.IsChecked = true;
                }
                else {
                    SkeletonStreamEnable.IsChecked = false;
                }
            }
            else {
                Kinect.SkeletonStream.Disable();
                trackingModes.IsEnabled = false;

                // To ensure that old skeletons aren't displayed when SkeletonTracking
                // is reenabled, we ask SkeletonViewer to hide them all now.
                DiagViewer.KinectSkeletonViewerOnColor.HideAllSkeletons();
                DiagViewer.KinectSkeletonViewerOnDepth.HideAllSkeletons();
                DiagViewer.KinectSkeletonViewerOnColor.Visibility = Visibility.Hidden;
                DiagViewer.KinectSkeletonViewerOnDepth.Visibility = Visibility.Hidden;
            }
        }

        private void TrackingModesSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = sender as ComboBox;
            if (comboBox == null) {
                return;
            }

            if (Kinect != null && Kinect.Status == KinectStatus.Connected && comboBox.SelectedItem != null) {
                var newMode = (TrackingMode) comboBox.SelectedItem;
                Kinect.SkeletonStream.AppChoosesSkeletons = newMode != TrackingMode.DefaultSystemTracking;
                DiagViewer.KinectSkeletonViewerOnColor.TrackingMode = newMode;
                DiagViewer.KinectSkeletonViewerOnDepth.TrackingMode = newMode;
            }
        }

        private void DepthRangesSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = sender as ComboBox;
            if (comboBox == null) {
                return;
            }

            if (Kinect != null && Kinect.Status == KinectStatus.Connected && comboBox.SelectedItem != null) {
                try {
                    Kinect.DepthStream.Range = (DepthRange) comboBox.SelectedItem;
                }
                catch (InvalidOperationException) {
                    comboBox.SelectedIndex = 0;
                    comboBox.Items.RemoveAt(1);
                    comboBox.Items.Add("-- NearMode not supported on this device. See Readme. --");
                }
                catch (InvalidCastException) {
                    // they chose the error string, switch back
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        private void ColorStreamEnabled(object sender, RoutedEventArgs e) {
            var checkBox = (CheckBox) sender;
            DisplayColumnBasedOnIsChecked(checkBox, 1, 2);
            DisplayPanelBasedOnIsChecked(checkBox, DiagViewer.colorPanel);
            if (Kinect != null) {
                EnableColorImageStreamBasedOnIsChecked(checkBox, Kinect.ColorStream, colorFormats);
            }
        }

        private void EnableDepthStreamBasedOnDepthOrSkeletonEnabled(
            DepthImageStream depthImageStream, ComboBox depthFormatsValue) {
            if (depthFormatsValue.SelectedItem != null) {
                // SkeletonViewer needs depth. So if DepthViewer or SkeletonViewer is enabled, enabled depthStream.
                if ((DepthStreamEnable.IsChecked.HasValue && DepthStreamEnable.IsChecked.Value)
                    || (SkeletonStreamEnable.IsChecked.HasValue && SkeletonStreamEnable.IsChecked.Value)) {
                    depthImageStream.Enable((DepthImageFormat) depthFormatsValue.SelectedItem);
                }
                else {
                    depthImageStream.Disable();
                }
            }
        }

        private void EnableColorImageStreamBasedOnIsChecked(
            CheckBox checkBox, ColorImageStream imageStream, ComboBox colorFormatsValue) {
            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value) {
                imageStream.Enable((ColorImageFormat) colorFormatsValue.SelectedItem);
            }
            else {
                imageStream.Disable();
            }
        }

        private void DepthStreamEnabled(object sender, RoutedEventArgs e) {
            var checkBox = (CheckBox) sender;
            DisplayColumnBasedOnIsChecked(checkBox, 2, 1);
            DisplayPanelBasedOnIsChecked(checkBox, DiagViewer.depthPanel);
            if (Kinect != null) {
                EnableDepthStreamBasedOnDepthOrSkeletonEnabled(Kinect.DepthStream, depthFormats);
            }
        }

        private void DisplayPanelBasedOnIsChecked(CheckBox checkBox, Grid panel) {
            // on load of XAML page, panel will be null.
            if (panel == null) {
                return;
            }

            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value) {
                panel.Visibility = Visibility.Visible;
            }
            else {
                panel.Visibility = Visibility.Collapsed;
            }
        }

        private void DisplayColumnBasedOnIsChecked(CheckBox checkBox, int column, int stars) {
            if (checkBox.IsChecked.HasValue && checkBox.IsChecked.Value) {
                DiagViewer.LayoutRoot.ColumnDefinitions[column].Width = new GridLength(stars, GridUnitType.Star);
            }
            else {
                DiagViewer.LayoutRoot.ColumnDefinitions[column].Width = new GridLength(0);
            }
        }

        private void ElevationAngleChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (userUpdate) {
                debounce.Stop();
                debounce.Start();
            }
        }

        private void DebounceElapsed(object sender, EventArgs e) {
            // The time has elapsed.  We may start it again later.
            debounce.Stop();

            var angleToSet = (int) ElevationAngle.Value;

            // Is there an update in progress?
            if (backgroundUpdateInProgress) {
                // Try again in a few moments.
                debounce.Start();
            }
            else {
                backgroundUpdateInProgress = true;

                Task.Factory.StartNew(
                    () => {
                        try {
                            // Check for not null and running
                            if ((Kinect != null) && Kinect.IsRunning) {
                                // We must wait at least 1 second, and call no more frequently than 15 times every 20 seconds
                                // So, we wait at least 1350ms afterwards before we set backgroundUpdateInProgress to false.
                                Kinect.ElevationAngle = angleToSet;
                                lastSetSensorAngle = angleToSet;
                                Thread.Sleep(1350);
                            }
                        }
                        finally {
                            backgroundUpdateInProgress = false;
                        }
                    }).ContinueWith(
                        results => {
                            // This can happen if the Kinect transitions from Running to not running
                            // after the check above but before setting the ElevationAngle.
                            if (results.IsFaulted) {
                                AggregateException exception = results.Exception;

                                Debug.WriteLine(
                                    "Set Elevation Task failed with exception " +
                                    exception);
                            }
                        });
            }
        }
    }
}