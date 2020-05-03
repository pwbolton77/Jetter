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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JetterPilot
{
    #region ChatControl class
    /// <summary>
    /// This class represents a signin control, which is used to create a new
    /// chatter. The user is expected to pick a name and an image. When they
    /// have done this a new <see cref="Common.Person">chatter </see>
    /// is created
    /// 
    /// This provides the logic for the user control, whilst the SignInControl.xaml
    /// provides the WPF UI design.
    /// </summary>
    public partial class SignInControl : System.Windows.Controls.UserControl
    {
        #region Instance Fields
        string login_name;
        #endregion
        #region Routed Events

        /// <summary>
        /// LoginButtonClickEvent event, occurs when the user clicks the add button
        /// </summary>
        public static readonly RoutedEvent LoginButtonClickEvent = EventManager.RegisterRoutedEvent(
            "LoginButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SignInControl));

        /// <summary>
        /// Expose the LoginButtonClick to external sources
        /// </summary>
        public event RoutedEventHandler LoginButtonClick
        {
            add { AddHandler(LoginButtonClickEvent, value); }
            remove { RemoveHandler(LoginButtonClickEvent, value); }
        }

        #endregion
        #region Constructor
        /// <summary>
        /// Blank constructor
        /// </summary>
        public SignInControl()
        {
            InitializeComponent();
        }

        #endregion
        #region Public Properties
        /// <summary>
        /// The current <see cref="Common.Person">chatter</see>
        /// </summary>
        public string LoginName 
        {
            get { return login_name; }
            set { login_name = value; }
        }
        #endregion
        #region Private Methods
        /// <summary>
        /// If there is a chatter name and image provided
        /// the currentPerson property is set to be a new
        /// <see cref="Common.Person">chatter</see> and the
        /// LoginButtonClickEvent is raised which is used by the
        /// parent <see cref="Window1">window</see>
        /// </summary>
        /// <param name="sender">LoginButton</param>
        /// <param name="e">The event args</param>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginCommand();
        }

        void LoginCommand()
        {
            if (!string.IsNullOrEmpty(txtName.Text))
            {
                login_name = txtName.Text;
                RaiseEvent(new RoutedEventArgs(LoginButtonClickEvent));
            }
            else
            {
                MessageBox.Show("You need to enter a pilot login name", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            txtName.Focus();
        }
    }
    #endregion
}
