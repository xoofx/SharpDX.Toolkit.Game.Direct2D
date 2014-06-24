using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace App1 {
    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        private readonly MyGame _myGame;

        public MainPage() {
            InitializeComponent();

            _myGame = new MyGame();
            Loaded += (s, e) => _myGame.Run(SwapChainPanel1);
        }
    }
}