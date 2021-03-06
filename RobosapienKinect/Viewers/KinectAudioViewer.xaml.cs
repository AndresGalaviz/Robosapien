//------------------------------------------------------------------------------
// <copyright file="KinectAudioViewer.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using Microsoft.Kinect;

namespace Com.Enterprisecoding.RobosapienKinect.Viewers {
    /// <summary>
    ///     Interaction logic for KinectAudioViewer.xaml
    /// </summary>
    public partial class KinectAudioViewer : ImageViewer {
        private double angle;
        private double soundSourceAngle;

        public KinectAudioViewer() {
            InitializeComponent();
            MarkWidth = 0.05;
            SoundSourceWidth = 0.05;
            BeamAngleInDegrees = 0;
            BeamDisplayText = null;
            SoundSourceAngleInDegrees = 0;
            SoundSourceDisplayText = null;
        }

        /// <summary>
        ///     Gets or sets string overlayed on beam indicator
        /// </summary>
        public string BeamDisplayText {
            get { return txtDisplayBeam.Text; }

            set { txtDisplayBeam.Text = value; }
        }

        /// <summary>
        ///     Gets or sets string overlayed on sound source indicator
        /// </summary>
        public string SoundSourceDisplayText {
            get { return txtDisplaySource.Text; }

            set { txtDisplaySource.Text = value; }
        }

        /// <summary>
        ///     Gets or sets width of the beam mark, in the 0-0.5 range
        /// </summary>
        public double MarkWidth { get; set; }

        /// <summary>
        ///     Gets or sets audio beam angle, in degrees
        /// </summary>
        public double BeamAngleInDegrees {
            get { return angle; }

            set {
                // save RAW sensor value
                angle = value;

                // Angle is in Degrees, so map the MinBeamAngle..MaxBeamAngle range to 0..1
                // and clamp
                double gradientOffset = (value/(KinectAudioSource.MaxBeamAngle - KinectAudioSource.MinBeamAngle)) + 0.5;
                if (gradientOffset > 1.0) {
                    gradientOffset = 1.0;
                }

                if (gradientOffset < 0.0) {
                    gradientOffset = 0.0;
                }

                // Move the gradient stops together
                gsPre.Offset = Math.Max(gradientOffset - MarkWidth, 0);
                gsIt.Offset = gradientOffset;
                gsPos.Offset = Math.Min(gradientOffset + MarkWidth, 1);
            }
        }

        /// <summary>
        ///     Gets or sets width of the sound source mark, in the 0-0.5 range
        /// </summary>
        public double SoundSourceWidth { get; set; }

        /// <summary>
        ///     Gets or sets sound direction angle, in degrees
        /// </summary>
        public double SoundSourceAngleInDegrees {
            get { return soundSourceAngle; }

            set {
                // save RAW sensor value
                soundSourceAngle = value;

                // Angle is in Degrees, so map the MinSoundSourceAngle..MaxSoundSourceAngle range to 0..1
                // and clamp
                double gradientOffset = (value
                                         /
                                         (KinectAudioSource.MaxSoundSourceAngle - KinectAudioSource.MinSoundSourceAngle))
                                        + 0.5;
                if (gradientOffset > 1.0) {
                    gradientOffset = 1.0;
                }

                if (gradientOffset < 0.0) {
                    gradientOffset = 0.0;
                }

                // Move the gradient stops together
                gsPreS.Offset = Math.Max(gradientOffset - SoundSourceWidth, 0);
                gsItS.Offset = gradientOffset;
                gsPosS.Offset = Math.Min(gradientOffset + SoundSourceWidth, 1);
            }
        }

        protected override void OnKinectChanged(KinectSensor oldKinectSensor, KinectSensor newKinectSensor) {
            if (oldKinectSensor != null && oldKinectSensor.AudioSource != null) {
                // remove old handlers
                oldKinectSensor.AudioSource.BeamAngleChanged -= AudioSourceBeamChanged;
                oldKinectSensor.AudioSource.SoundSourceAngleChanged -= AudioSourceSoundSourceAngleChanged;
            }

            if (newKinectSensor != null && newKinectSensor.AudioSource != null) {
                // add new handlers
                newKinectSensor.AudioSource.BeamAngleChanged += AudioSourceBeamChanged;
                newKinectSensor.AudioSource.SoundSourceAngleChanged += AudioSourceSoundSourceAngleChanged;
            }
        }

        private void AudioSourceSoundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e) {
            // Set width of mark based on confidence
            SoundSourceWidth = Math.Max(((1 - e.ConfidenceLevel)/2), 0.02);

            // Move indicator
            SoundSourceAngleInDegrees = e.Angle;

            // Update text
            SoundSourceDisplayText = " Sound source angle = " + SoundSourceAngleInDegrees.ToString("0.00") + " deg  Confidence level=" + e.ConfidenceLevel.ToString("0.00");
        }

        private void AudioSourceBeamChanged(object sender, BeamAngleChangedEventArgs e) {
            // Move our indicator
            BeamAngleInDegrees = e.Angle;

            // Update Text
            BeamDisplayText = " Audio beam angle = " + BeamAngleInDegrees.ToString("0.00") + " deg";
        }
    }
}