using System;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Threading;

// https://github.com/yadyn/WPF-SearchTextBox
namespace BsaBrowser.Controls
{
    /// <summary>
	/// Defines a list of modes for the <see cref="SearchTextBox"/> control.
	/// </summary>
	public enum SearchTextBoxMode
    {
        /// <summary>
        /// Command will be fired automatically as the user types. The default
        /// is to fire on every keystroke, but you can optionally define the
        /// InstantSearchDelay.
        /// </summary>
        Instant,
        /// <summary>
        /// Command will be fired only when the search button is explicitly
        /// clicked or when the user presses Enter while focus is in the
        /// textbox.
        /// </summary>
        Regular
    }

    /// <summary>
	/// Represents a control that can be used for text search input.
	/// </summary>
	[TemplatePart(Name = "PART_ContentHost", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_PromptHost", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_SearchButtonHost", Type = typeof(ButtonBase))]
    [TemplatePart(Name = "PART_ClearButtonHost", Type = typeof(ButtonBase))]
    public class SearchTextBox : TextBox, ICommandSource
    {
        #region Dependency Properties
        /// <summary>
        /// Identifies the <see cref="AllowEmptySearches" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowEmptySearchesProperty = DependencyProperty.Register(
                "AllowEmptySearches", typeof(bool), typeof(SearchTextBox),
                new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnAllowEmptySearchesChanged)));

        /// <summary>
		/// Gets or sets a value indicating whether empty text searches are allowed.
		/// </summary>
		/// <value>The default value is <c>true</c>.</value>
		/// <remarks>
		/// If set to <c>false</c>, no Search event or command execution will
		/// occur if the text is empty and the Search button will be disabled
		/// while the text is empty.
		/// </remarks>
		[Category("Common Properties")]
        public bool AllowEmptySearches
        {
            get => (bool)GetValue(AllowEmptySearchesProperty);
            set { SetValue(AllowEmptySearchesProperty, value); }
        }

        private static void OnAllowEmptySearchesChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            bool allowEmptySearches = (bool)e.NewValue;
            if (o is SearchTextBox stb && stb._searchButtonHost != null)
            {
                if (allowEmptySearches)
                {
                    stb._searchButtonHost.IsEnabled = (stb.Command == null) ? true : stb.Command.CanExecute(stb.InternalCommandParameter);
                }
                else
                {
                    stb.UpdateSearchButtonIsEnabled();
                }
            }
        }

        /// <summary>
        /// Identifies the <see cref="Command" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
                "Command", typeof(ICommand), typeof(SearchTextBox),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCommandPropsChanged)));

        /// <summary>
		/// Gets or sets the command to invoke when the search button is pressed or during instant search.
		/// </summary>
		[Bindable(true),
        Localizability(LocalizationCategory.NeverLocalize)]
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set { SetValue(CommandProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
                "CommandParameter", typeof(object), typeof(SearchTextBox),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCommandPropsChanged)));

        /// <summary>
		/// Gets or sets the parameter to pass to the <see cref="P:Command"/> property.
		/// </summary>
		[Bindable(true),
        Localizability(LocalizationCategory.NeverLocalize)]
        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set { SetValue(CommandParameterProperty, value); }
        }

        private object InternalCommandParameter
        {
            get => CommandParameter ?? this;
        }

        /// <summary>
        /// Identifies the <see cref="CommandTarget" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
                "CommandTarget", typeof(IInputElement), typeof(SearchTextBox),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnCommandPropsChanged)));

        /// <summary>
		/// Gets or sets the element on which to raise the specified command.
		/// </summary>
		[Bindable(true)]
        public IInputElement CommandTarget
        {
            get => (IInputElement)GetValue(CommandTargetProperty);
            set { SetValue(CommandTargetProperty, value); }
        }

        private static void OnCommandPropsChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            SearchTextBox stb = o as SearchTextBox;
            if (stb != null)
            {
                stb.UpdateSearchButtonCommand();
                stb.UpdateSearchButtonIsEnabled();
            }
        }

        private static readonly DependencyPropertyKey HasTextPropertyKey = DependencyProperty.RegisterReadOnly(
                "HasText", typeof(bool), typeof(SearchTextBox),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// Identifies the <see cref="HasText" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasTextProperty = HasTextPropertyKey.DependencyProperty;

        /// <summary>
		/// Gets a value indicating whether this control has text entered or not.
		/// </summary>
		[Browsable(false)]
        public bool HasText
        {
            get => (bool)GetValue(HasTextProperty);
            private set { SetValue(HasTextPropertyKey, value); }
        }

        /// <summary>
        /// Identifies the <see cref="InstantSearchDelay" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty InstantSearchDelayProperty = DependencyProperty.Register(
                "InstantSearchDelay", typeof(Duration), typeof(SearchTextBox),
                new FrameworkPropertyMetadata(new Duration(TimeSpan.Zero), new PropertyChangedCallback(OnInstantSearchDelayChanged)));

        /// <summary>
		/// Gets or sets the delay between firing command during instant search mode.
		/// </summary>
		[Category("Common Properties")]
        public Duration InstantSearchDelay
        {
            get => (Duration)GetValue(InstantSearchDelayProperty);
            set { SetValue(InstantSearchDelayProperty, value); }
        }

        private static void OnInstantSearchDelayChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SearchTextBox stb)
            {
                stb._searchDelayTimer.Interval = ((Duration)e.NewValue).TimeSpan;
                stb._searchDelayTimer.Stop();
            }
        }

        /// <summary>
        /// Identifies the <see cref="Prompt" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty PromptProperty = DependencyProperty.Register(
                "Prompt", typeof(object), typeof(SearchTextBox),
                new FrameworkPropertyMetadata("Search", FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
		/// Gets or sets content to display as a prompt when the textbox is empty.
		/// </summary>
		/// <remarks>
		/// The default is Search.
		/// </remarks>
		[Category("Common Properties")]
        public object Prompt
        {
            get => GetValue(PromptProperty);
            set { SetValue(PromptProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PromptTemplate" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty PromptTemplateProperty = DependencyProperty.Register(
                "PromptTemplate", typeof(DataTemplate), typeof(SearchTextBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
		/// Gets or sets the template to use for the prompt content.
		/// </summary>
		public DataTemplate PromptTemplate
        {
            get => (DataTemplate)GetValue(PromptTemplateProperty);
            set { SetValue(PromptTemplateProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SearchMode" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchModeProperty = DependencyProperty.Register(
                "SearchMode", typeof(SearchTextBoxMode), typeof(SearchTextBox),
                new FrameworkPropertyMetadata(SearchTextBoxMode.Instant, 
                    FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
		/// Gets or sets the search behavior of the textbox.
		/// </summary>
		/// <value>
		/// One of the <see cref="SearchTextBoxMode"/> values. The default is Instant.
		/// </value>
		[Category("Common Properties")]
        public SearchTextBoxMode SearchMode
        {
            get => (SearchTextBoxMode)GetValue(SearchModeProperty);
            set { SetValue(SearchModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Search"/> routed event.
        /// </summary>
        [Category("Behavior")]
        public static readonly RoutedEvent SearchEvent = EventManager.RegisterRoutedEvent(
                "Search", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SearchTextBox));

        /// <summary>
		/// Occurs when the search button is pressed or during instant search.
		/// </summary>
		public event RoutedEventHandler Search
        {
            add { AddHandler(SearchEvent, value); }
            remove { RemoveHandler(SearchEvent, value); }
        }
        #endregion

        #region Variables
        private ButtonBase _searchButtonHost;
        private ButtonBase _clearButtonHost;

        private DispatcherTimer _searchDelayTimer;
        #endregion

        #region Constructors
        // <summary>
        /// Initializes the <see cref="SearchTextBox"/> class.
        /// </summary>
        static SearchTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchTextBox), new FrameworkPropertyMetadata(typeof(SearchTextBox)));
            TextProperty.OverrideMetadata(typeof(SearchTextBox), new FrameworkPropertyMetadata(String.Empty, new PropertyChangedCallback(OnTextPropertyChanged)));
        }

        /// <summary>
		/// Initializes a new instance of the <see cref="SearchTextBox"/> class.
		/// </summary>
		public SearchTextBox()
        {
            _searchDelayTimer = new DispatcherTimer();
            _searchDelayTimer.Interval = TimeSpan.Zero;
            _searchDelayTimer.Tick += HandleSearchDelayTimerTick;
        }
        #endregion

        #region Methods
        private static void OnTextPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is SearchTextBox stb)
            {
                stb.UpdateSearchButtonIsEnabled();
            }
        }

        /// <summary>
		/// Resets search text to empty. This will both raise the Search event
		/// and execute any bound command.
		/// </summary>
		public void Reset()
        {
            Text = string.Empty;
            _searchDelayTimer.Stop();
            RaiseSearchEvent();
            ExecuteSearchCommand();
        }

        /// <summary>
		/// Is called when a control template is applied.
		/// </summary>
		public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_searchButtonHost != null)
            {
                _searchButtonHost.Click -= HandleSearchButtonClick;
            }
            if (_clearButtonHost != null)
            {
                _clearButtonHost.Click -= HandleClearButtonClick;
            }

            _searchButtonHost = GetTemplateChild("PART_SearchButtonHost") as ButtonBase;
            _clearButtonHost = GetTemplateChild("PART_ClearButtonHost") as ButtonBase;

            if (_searchButtonHost != null)
            {
                _searchButtonHost.Click += HandleSearchButtonClick;
                UpdateSearchButtonCommand();
                UpdateSearchButtonIsEnabled();
            }
            if (_clearButtonHost != null)
            {
                _clearButtonHost.Click += HandleClearButtonClick;
            }
        }

        /// <summary>
		/// Invoked whenever an unhandled <see cref="E:System.Windows.Input.Keyboard.KeyDown"/> attached routed event reaches an element derived from this class in its route. Implement this method to add class handling for this event.
		/// </summary>
		/// <param name="e">Provides data about the event.</param>
		protected override void OnKeyDown(KeyEventArgs e)
        {
            if (SearchMode == SearchTextBoxMode.Instant && e.Key == Key.Escape)
            {
                Text = string.Empty;

                e.Handled = true;
            }
            else if (SearchMode == SearchTextBoxMode.Regular && (e.Key == Key.Return || e.Key == Key.Enter))
            {
                RaiseSearchEvent();
                ExecuteSearchCommand();

                e.Handled = true;
            }
            else
            {
                base.OnKeyDown(e);
            }
        }

        /// <summary>
		/// Is called when content in this editing control changes.
		/// </summary>
		/// <param name="e">The arguments that are associated with the <see cref="E:System.Windows.Controls.Primitives.TextBoxBase.TextChanged"/> event.</param>
		protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);

            HasText = !string.IsNullOrEmpty(Text);

            UpdateSearchButtonIsEnabled();

            if (SearchMode == SearchTextBoxMode.Instant)
            {
                _searchDelayTimer.Stop();
                _searchDelayTimer.Start();
            }
        }

        /// <summary>
		/// Raises the <see cref="E:Search"/> event.
		/// </summary>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="e"/> relates to an event other than <see cref="SearchEvent"/>.
		/// </exception>
		protected void OnSearch(RoutedEventArgs e)
        {
            if (e.RoutedEvent != SearchEvent)
                throw new ArgumentException("Only RoutedEventArgs for the SearchEvent may be passed in.", "e");
            RaiseEvent(e);
        }

        private void HandleCommandCanExecuteChanged(object sender, EventArgs e)
        {
            UpdateSearchButtonIsEnabled();
        }

        private void HandleSearchDelayTimerTick(object sender, EventArgs e)
        {
            _searchDelayTimer.Stop();
            RaiseSearchEvent();
            ExecuteSearchCommand();
        }

        private void HandleSearchButtonClick(object sender, RoutedEventArgs e)
        {
            RaiseSearchEvent();
        }

        private void HandleClearButtonClick(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void RaiseSearchEvent()
        {
            if (AllowEmptySearches || HasText)
                OnSearch(new RoutedEventArgs(SearchEvent));
        }

        private void ExecuteSearchCommand()
        {
            if ((AllowEmptySearches || HasText) && Command != null && Command.CanExecute(InternalCommandParameter))
            {
                Command.Execute(InternalCommandParameter);
            }
                
        }

        private void UpdateSearchButtonCommand()
        {
            if (_searchButtonHost != null)
            {
                if (_searchButtonHost.Command != null)
                {
                    _searchButtonHost.Command.CanExecuteChanged -= HandleCommandCanExecuteChanged;
                }

                _searchButtonHost.Command = Command;
                _searchButtonHost.CommandParameter = CommandParameter;
                _searchButtonHost.CommandTarget = CommandTarget;

                if (_searchButtonHost.Command != null)
                {
                    _searchButtonHost.Command.CanExecuteChanged += HandleCommandCanExecuteChanged;
                }
            }
        }

        private void UpdateSearchButtonIsEnabled()
        {
            if (!AllowEmptySearches && _searchButtonHost != null)
            {
                if (Command == null)
                {
                    _searchButtonHost.IsEnabled = HasText;
                }
                else
                {
                    _searchButtonHost.IsEnabled = HasText && Command.CanExecute(InternalCommandParameter);
                }
            }
        }
        #endregion
    }
}
