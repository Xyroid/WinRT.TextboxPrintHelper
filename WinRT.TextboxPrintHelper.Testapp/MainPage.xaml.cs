using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace WinRT.TextboxPrintHelper.Testapp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string demoText = @"1. Man lives in the sunlit world of what he believes to be reality. But, there is, unseen by most, an underworld, a place that is just as real, but not as brightly lit... a darkside. The darkside is always there, waiting for us to enter, waiting to enter us. So until next time, try to enjoy the daylight.

2. Dance your cares away, worry's for another day. Let the music play, down at Fraggle Rock! Work you cares away, dancing's for another day. Let the Fraggles play, We're Gobo, Mokey, Wembley, Boober, Red! Dance your cares away, worry's for another day. Let the music play, down at Fraggle Rock! Down at Fraggle Rock! Down at Fraggle Rock.

3. Once upon there were three little girls who went to the police academy. Two in Los Angeles. The other in San Francisco. And they were each assigned very hazardous duties. But I took them away from all that. And now they work for me. My name is Charlie.

4. Well we're movin' on up, to the east side. To a deluxe apartment in the sky. Movin' on up, To the east side. We finally got a piece of the pie. Fish don't fry in the kitchen, beans don't burn on the grill. Took a whole lotta tryin' just to get up that hill. Now we're up in the big leagues, gettin' our turn at bat. As long as we live, it's you and me baby, There ain't nothin' wrong with that. Well we're movin' on up, to the east side. To a deluxe apartment in the sky. Movin' on up, to the east side. We finally got a piece of the pie.

5. Now, the world don't move to the beat of just one drum, what might be right for you, may not be right for some. A man is born, he's a man of means. Then along come two, they got nothing but their jeans. But they got, Diff'rent Strokes, it takes Diff'rent Strokes. It takes Diff'rent Strokes to move the world.

6. Extraordinary crimes against the people and the state had to be avenged by agents extraordinary. Two such people are John Steed, top professional; and his partner, Emma Peel, talented amateur... otherwise known as The Avengers.";
            
            MyTextBox.Text = demoText;
        }

        private async void OnPrint_Click(object sender, RoutedEventArgs e)
        {
            await Printer.ShowPrintUIAsync(PrintPreview, MyTextBox, Guid.NewGuid().ToString());
        }
    }
}
