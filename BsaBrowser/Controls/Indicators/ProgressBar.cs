// -----------------------------------------------------------------------
// <copyright file="ProgressBar.cs" company="Anori Soft">
// Copyright (c) Anori Soft. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BsaBrowser.Controls
{
    /// <summary>
    /// </summary>
    /// <seealso cref="System.Windows.Controls.ProgressBar" />
    [TemplateVisualState(Name = StateDeterminate, GroupName = GroupCommon)]
    [TemplateVisualState(Name = StateIndeterminate, GroupName = GroupCommon)]
    public class ProgressBar : System.Windows.Controls.ProgressBar
    {
        /// <summary>
        ///     The group common
        /// </summary>
        internal const string GroupCommon = "CommonStates";

        /// <summary>
        ///     The state determinate
        /// </summary>
        internal const string StateDeterminate = "Determinate";

        /// <summary>
        ///     The state indeterminate
        /// </summary>
        internal const string StateIndeterminate = "Indeterminate";

        /// <summary>
        ///     Identifies the SkipValue Property.
        /// </summary>
        public static readonly DependencyProperty SkipValueProperty = DependencyProperty.Register(
            "SkipValue",
            typeof(double),
            typeof(ProgressBar),
            new PropertyMetadata(0.0, OnSkipValueChanged));

        /// <summary>
        ///     Identifies the IsIndeterminate Property.
        /// </summary>
        public new static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
            "IsIndeterminate",
            typeof(bool),
            typeof(ProgressBar),
            new PropertyMetadata(OnIsIndeterminateChanged));

        /// <summary>
        ///     Gets or sets the element indicator.
        /// </summary>
        /// <value>
        ///     The element indicator.
        /// </value>
        internal FrameworkElement ElementIndicator { get; set; }

        /// <summary>
        ///     Gets or sets the element track.
        /// </summary>
        /// <value>
        ///     The element track.
        /// </value>
        internal FrameworkElement ElementTrack { get; set; }

        /// <summary>
        ///     Gets or sets the element spacer.
        /// </summary>
        /// <value>
        ///     The element spacer.
        /// </value>
        internal FrameworkElement ElementSpacer { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating the offset from which the ProgressBar indicator should start from.
        ///     This is a dependency property.
        /// </summary>
        public double SkipValue
        {
            get => (double)this.GetValue(SkipValueProperty);
            set => this.SetValue(SkipValueProperty, value);
        }

        /// <summary>
        ///     Sets the length of the progress bar indicator.
        /// </summary>
        private void SetProgressBarIndicatorLength()
        {
            var minimum = this.Minimum;
            var maximum = this.Maximum;
            var progressValue = this.Value;
            if ((this.ElementTrack == null) || (this.ElementIndicator == null))
            {
                return;
            }

            if (!(VisualTreeHelper.GetParent(this.ElementIndicator) is FrameworkElement parent))
            {
                return;
            }

            var indicatorMargins = this.ElementIndicator.Margin.Left + this.ElementIndicator.Margin.Right;
            switch (parent)
            {
                case Border border:
                    indicatorMargins += border.Padding.Left + border.Padding.Right;
                    break;

                case Control control:
                    indicatorMargins += control.Padding.Left + control.Padding.Right;
                    break;
            }

            var filledAreaRatio = (this.IsIndeterminate || (maximum == minimum))
                                      ? 1.0
                                      : ((progressValue - minimum) / (maximum - minimum));
            var totalArea = Math.Max(0.0, parent.ActualWidth - indicatorMargins);
            if (this.ElementSpacer != null)
            {
                this.ElementSpacer.Width = totalArea * this.GetSkipRatio();
                totalArea -= this.ElementSpacer.Width;
            }

            this.ElementIndicator.Width = filledAreaRatio * totalArea;
        }

        /// <summary>
        ///     Gets the range.
        /// </summary>
        /// <returns></returns>
        internal double GetRange() => this.Maximum - this.Minimum;

        /// <summary>
        ///     Gets the skip ratio.
        /// </summary>
        /// <returns></returns>
        internal double GetSkipRatio() => this.SkipValue / this.GetRange();

        /// <summary>
        ///     Called when [skip value changed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        ///     The <see cref="System.Windows.DependencyPropertyChangedEventArgs" /> instance containing the event
        ///     data.
        /// </param>
        private static void OnSkipValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var progressBar = sender as ProgressBar;

            Debug.Assert(progressBar != null, "The Sender Should be an instance of a RadProgressBar");

            progressBar.SetProgressBarIndicatorLength();
        }

        private static void OnIsIndeterminateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bar = (ProgressBar)d;
            if (bar == null)
            {
                return;
            }

            void BarLayoutUpdate(object sender, EventArgs args)
            {
                bar.SetProgressBarIndicatorLength();
                bar.LayoutUpdated -= BarLayoutUpdate;
            }

            // This is to ensure that the size of the IndicatorGrid is updated. Issue with tfs ID: 218490.
            bar.LayoutUpdated += BarLayoutUpdate;
            bar.UpdateVisualState(false);
            bar.IsIndeterminate = (bool)e.NewValue;
        }

        /// <summary>
        ///     Goes to state.
        /// </summary>
        /// <param name="useTransitions">if set to <c>true</c> [use transitions].</param>
        /// <param name="stateName">Name of the state.</param>
        private void GoToState(bool useTransitions, string stateName) => VisualStateManager.GoToState(this, stateName, useTransitions);

        /// <summary>
        ///     Raises the <see cref="E:PropertyChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property.Name == "Visibility")
            {
                this.UpdateVisualState(false);
            }
        }

        /// <summary>
        ///     Updates the state of the visual.
        /// </summary>
        /// <param name="useTransitions">if set to <c>true</c> [use transitions].</param>
        internal void UpdateVisualState(bool useTransitions)
        {
            if (this.Orientation == Orientation.Vertical)
            {
                this.GoToState(useTransitions, "Vertical");
            }
            else
            {
                this.GoToState(useTransitions, "Horizontal");
            }

            if (this.Visibility != Visibility.Visible)
            {
                this.GoToState(useTransitions, StateDeterminate);
                return;
            }

            if (!this.IsIndeterminate)
            {
                this.GoToState(useTransitions, StateDeterminate);
            }
            else
            {
                this.GoToState(useTransitions, StateIndeterminate);
            }
        }
    }
}