// -----------------------------------------------------------------------
// <copyright file="BusyIndicator.cs" company="Anori Soft">
// Copyright (c) Anori Soft. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace BsaBrowser.Controls
{
    /// <summary>
    ///     A control to provide a visual indicator when an application is busy.
    /// </summary>
    [TemplateVisualState(Name = VisualStates.StateIdle, GroupName = VisualStates.GroupBusyStatus)]
    [TemplateVisualState(Name = VisualStates.StateBusy, GroupName = VisualStates.GroupBusyStatus)]
    [TemplateVisualState(Name = VisualStates.StateVisible, GroupName = VisualStates.GroupVisibility)]
    [TemplateVisualState(Name = VisualStates.StateHidden, GroupName = VisualStates.GroupVisibility)]
    [StyleTypedProperty(Property = "OverlayStyle", StyleTargetType = typeof(Rectangle))]
    [StyleTypedProperty(Property = "ProgressBarStyle", StyleTargetType = typeof(ProgressBar))]
    public class BusyIndicator : ContentControl
    {
        /// <summary>
        ///     Identifies the BusyContent dependency property.
        /// </summary>
        public static readonly DependencyProperty BusyContentProperty = DependencyProperty.Register(
            "BusyContent",
            typeof(object),
            typeof(BusyIndicator),
            new PropertyMetadata(null));

        /// <summary>
        ///     Identifies the BusyTemplate dependency property.
        /// </summary>
        public static readonly DependencyProperty BusyContentTemplateProperty = DependencyProperty.Register(
            "BusyContentTemplate",
            typeof(DataTemplate),
            typeof(BusyIndicator),
            new PropertyMetadata(null));

        /// <summary>
        ///     Identifies the DisplayAfter dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayAfterProperty = DependencyProperty.Register(
            "DisplayAfter",
            typeof(TimeSpan),
            typeof(BusyIndicator),
            new PropertyMetadata(TimeSpan.FromSeconds(0.1)));

        /// <summary>
        ///     Identifies the FocusAfterBusy dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusAfterBusyProperty = DependencyProperty.Register(
            "FocusAfterBusy",
            typeof(Control),
            typeof(BusyIndicator),
            new PropertyMetadata(null));

        /// <summary>
        ///     Identifies the IsBusy dependency property.
        /// </summary>
        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            "IsBusy",
            typeof(bool),
            typeof(BusyIndicator),
            new PropertyMetadata(false, OnIsBusyChanged));

        /// <summary>
        ///     Identifies the OverlayStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty OverlayStyleProperty = DependencyProperty.Register(
            "OverlayStyle",
            typeof(Style),
            typeof(BusyIndicator),
            new PropertyMetadata(null));

        /// <summary>
        ///     Identifies the ProgressBarStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty ProgressBarStyleProperty = DependencyProperty.Register(
            "ProgressBarStyle",
            typeof(Style),
            typeof(BusyIndicator),
            new PropertyMetadata(null));

        /// <summary>
        ///     The progress value property
        /// </summary>
        public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register(
            "ProgressValue",
            typeof(double),
            typeof(BusyIndicator),
            new PropertyMetadata(null));

        /// <summary>
        ///     The is indeterminate dependency property.
        /// </summary>
        public static readonly DependencyProperty IsIndeterminateProperty = DependencyProperty.Register(
            "IsIndeterminate",
            typeof(bool),
            typeof(BusyIndicator),
            new PropertyMetadata(true, null));

        /// <summary>
        ///     Timer used to delay the initial display and avoid flickering.
        /// </summary>
        private readonly DispatcherTimer displayAfterTimer = new DispatcherTimer();

        /// <summary>
        ///     Initializes the <see cref="BusyIndicator" /> class.
        /// </summary>
        static BusyIndicator() =>
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(BusyIndicator),
                new FrameworkPropertyMetadata(typeof(BusyIndicator)));

        /// <summary>
        ///     Initializes a new instance of the <see cref="BusyIndicator" /> class.
        /// </summary>
        public BusyIndicator() => this.displayAfterTimer.Tick += this.DisplayAfterTimerElapsed;

        /// <summary>
        ///     Finalizes an instance of the <see cref="BusyIndicator" /> class.
        /// </summary>
        ~BusyIndicator() => this.displayAfterTimer.Tick -= this.DisplayAfterTimerElapsed;

        /// <summary>
        ///     Gets or sets a value indicating the busy content to display to the user.
        /// </summary>
        /// <value>
        ///     The content of the busy.
        /// </value>
        public object BusyContent
        {
            get => this.GetValue(BusyContentProperty);
            set => this.SetValue(BusyContentProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating the template to use for displaying the busy content to the user.
        /// </summary>
        /// <value>
        ///     The busy content template.
        /// </value>
        public DataTemplate BusyContentTemplate
        {
            get => (DataTemplate)this.GetValue(BusyContentTemplateProperty);
            set => this.SetValue(BusyContentTemplateProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating how long to delay before displaying the busy content.
        /// </summary>
        /// <value>
        ///     The display after.
        /// </value>
        public TimeSpan DisplayAfter
        {
            get => (TimeSpan)this.GetValue(DisplayAfterProperty);
            set => this.SetValue(DisplayAfterProperty, value);
        }

        /// <summary>
        ///     Gets or sets a Control that should get the focus when the busy indicator disapears.
        /// </summary>
        /// <value>
        ///     The focus after busy.
        /// </value>
        public Control FocusAfterBusy
        {
            get => (Control)this.GetValue(FocusAfterBusyProperty);
            set => this.SetValue(FocusAfterBusyProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the busy indicator should show.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is busy; otherwise, <c>false</c>.
        /// </value>
        public bool IsBusy
        {
            get => (bool)this.GetValue(IsBusyProperty);
            set => this.SetValue(IsBusyProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating the style to use for the overlay.
        /// </summary>
        /// <value>
        ///     The overlay style.
        /// </value>
        public Style OverlayStyle
        {
            get => (Style)this.GetValue(OverlayStyleProperty);
            set => this.SetValue(OverlayStyleProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating the style to use for the progress bar.
        /// </summary>
        public Style ProgressBarStyle
        {
            get => (Style)this.GetValue(ProgressBarStyleProperty);
            set => this.SetValue(ProgressBarStyleProperty, value);
        }

        /// <summary>
        ///     Gets or sets the progress value.
        /// </summary>
        /// <value>
        ///     The progress value.
        /// </value>
        public double ProgressValue
        {
            get => (double)this.GetValue(ProgressValueProperty);
            set => this.SetValue(ProgressValueProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether this instance is indeterminate.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is indeterminate; otherwise, <c>false</c>.
        /// </value>
        public bool IsIndeterminate
        {
            get => (bool)this.GetValue(IsIndeterminateProperty);
            set => this.SetValue(IsIndeterminateProperty, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the BusyContent is visible.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is content visible; otherwise, <c>false</c>.
        /// </value>
        protected bool IsContentVisible { get; set; }

        /// <summary>
        ///     Overrides the OnApplyTemplate method.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            this.ChangeVisualState(false);
        }

        /// <summary>
        ///     Changes the control's visual state(s).
        /// </summary>
        /// <param name="useTransitions">True if state transitions should be used.</param>
        protected virtual void ChangeVisualState(bool useTransitions)
        {
            VisualStateManager.GoToState(
                this,
                this.IsBusy ? VisualStates.StateBusy : VisualStates.StateIdle,
                useTransitions);
            VisualStateManager.GoToState(
                this,
                this.IsContentVisible ? VisualStates.StateVisible : VisualStates.StateHidden,
                useTransitions);
        }

        /// <summary>
        ///     IsBusyProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnIsBusyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (this.IsBusy)
            {
                if (this.DisplayAfter.Equals(TimeSpan.Zero))
                {
                    this.IsContentVisible = true;
                }
                else
                {
                    this.displayAfterTimer.Interval = this.DisplayAfter;
                    this.displayAfterTimer.Start();
                }
            }
            else
            {
                this.displayAfterTimer.Stop();
                this.IsContentVisible = false;

                this.FocusAfterBusy?.Dispatch(DispatcherPriority.Input, () => this.FocusAfterBusy.Focus());
            }

            this.ChangeVisualState(true);
        }

        /// <summary>
        ///     IsBusyProperty property changed handler.
        /// </summary>
        /// <param name="d">BusyIndicator that changed its IsBusy.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((BusyIndicator)d).OnIsBusyChanged(e);

        /// <summary>
        ///     Handler for the DisplayAfterTimer.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void DisplayAfterTimerElapsed(object sender, EventArgs e)
        {
            this.displayAfterTimer.Stop();
            this.IsContentVisible = true;
            this.ChangeVisualState(true);
        }
    }
}
