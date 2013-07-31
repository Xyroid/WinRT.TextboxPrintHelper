using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Printing;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Printing;

namespace WinRT.TextboxPrintHelper
{
    /// <summary>
    /// A helper class to simplify printing content of textbox with its formatting. This class supports some properties of textbox like 
    /// CharacterSpacing, FontFamily, FontSize, FontStretch, FontStyle, FontWeight, Foreground, Language & TextAlignment
    /// </summary>
    public class Printer
    {
        /// <summary>
        /// Printing root property on each input page.
        /// </summary>
        private static Canvas PrintingRoot { get; set; }

        /// <summary>
        /// The textbox of which the text is to be printed.
        /// </summary>
        private static TextBox PrintingTextBox { get; set; }

        /// <summary>
        /// The file name for the document which is going to be print.
        /// </summary>
        private static string PrintingFileName { get; set; }

        /// <summary>
        /// Current preview page
        /// </summary>
        internal static int currentPreviewPage;

        /// <summary>
        /// PrintDocument is a Xaml object which converts some PrintManager functionality into paradigms
        /// which are used in Xaml (eg. callbacks into events with event handlers).
        /// </summary>
        private static PrintDocument printDocument = null;

        /// <summary>
        /// Marker interface for document source
        /// </summary>
        private static IPrintDocumentSource printDocumentSource = null;

        /// <summary>
        /// A list of UIElements used to store the print preview pages.  This gives easy access
        /// to any desired preview page.
        /// </summary>
        internal static List<PageLoadState> printPreviewPages = new List<PageLoadState>();

        /// <summary>
        /// The percent of app's margin width, content is set at 85% (0.85) of the area's width
        /// </summary>
        private const double ApplicationContentMarginLeft = 0.075;

        /// <summary>
        /// The percent of app's margin heigth, content is set at 94% (0.94) of tha area's height
        /// </summary>
        private const double ApplicationContentMarginTop = 0.03;

        /// <summary>
        /// Helper getter for text showing
        /// </summary>
        private static bool ShowText
        {
            get { return ((int)imageText & (int)DisplayContent.Text) == (int)DisplayContent.Text; }
        }

        /// <summary>
        /// A flag that determines if text & images are to be shown
        /// </summary>
        internal static DisplayContent imageText = DisplayContent.TextAndImages;

        /// <summary>
        /// This is the event handler for PrintManager.PrintTaskRequested.
        /// </summary>
        /// <param name="sender">PrintManager</param>
        /// <param name="e">PrintTaskRequestedEventArgs </param>
        private static void PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs e)
        {
            PrintTask printTask = e.Request.CreatePrintTask(PrintingFileName, sourceRequested => sourceRequested.SetSource(printDocumentSource));
        }

        /// <summary>
        /// This function registers the app for printing with Windows and sets up the necessary event handlers for the print process.
        /// </summary>
        private static void RegisterForPrinting()
        {
            // Create the PrintDocument.
            printDocument = new PrintDocument();

            // Save the DocumentSource.
            printDocumentSource = printDocument.DocumentSource;

            // Add an event handler which creates preview pages.
            printDocument.Paginate += CreatePrintPreviewPages;

            // Add an event handler which provides a specified preview page.
            printDocument.GetPreviewPage += GetPrintPreviewPage;

            // Add an event handler which provides all final print pages.
            printDocument.AddPages += AddPrintPages;


            // Create a PrintManager and add a handler for printing initialization.
            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested += PrintTaskRequested;
        }

        /// <summary>
        /// This function unregisters the app for printing with Windows.
        /// </summary>
        private static void UnregisterForPrinting()
        {
            // Set the instance of the PrintDocument to null.
            printDocument = null;

            // Remove the handler for printing initialization.
            PrintManager printMan = PrintManager.GetForCurrentView();
            printMan.PrintTaskRequested -= PrintTaskRequested;
        }

        private static event EventHandler pagesCreated;

        /// <summary>
        /// This is the event handler for PrintDocument.Paginate. It creates print preview pages for the app.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Paginate Event Arguments</param>
        private static void CreatePrintPreviewPages(object sender, PaginateEventArgs e)
        {
            // Clear the cache of preview pages 
            printPreviewPages.Clear();

            // Clear the printing root of preview pages
            PrintingRoot.Children.Clear();

            // This variable keeps track of the last RichTextBlockOverflow element that was added to a page which will be printed
            RichTextBlockOverflow lastRTBOOnPage;

            // Get the PrintTaskOptions
            PrintTaskOptions printingOptions = ((PrintTaskOptions)e.PrintTaskOptions);

            // Get the page description to deterimine how big the page is
            PrintPageDescription pageDescription = printingOptions.GetPageDescription(0);

            // We know there is at least one page to be printed. passing null as the first parameter to
            // AddOnePrintPreviewPage tells the function to add the first page.
            lastRTBOOnPage = AddOnePrintPreviewPage(null, pageDescription);

            // We know there are more pages to be added as long as the last RichTextBoxOverflow added to a print preview
            // page has extra content
            while (lastRTBOOnPage.HasOverflowContent)
            {
                lastRTBOOnPage = AddOnePrintPreviewPage(lastRTBOOnPage, pageDescription);
            }

            if (pagesCreated != null)
                pagesCreated.Invoke(printPreviewPages, null);

            // Report the number of preview pages created
            printDocument.SetPreviewPageCount(printPreviewPages.Count, PreviewPageCountType.Intermediate);
        }

        /// <summary>
        /// This is the event handler for PrintDocument.GetPrintPreviewPage. It provides a specific print preview page,
        /// in the form of an UIElement, to an instance of PrintDocument. PrintDocument subsequently converts the UIElement
        /// into a page that the Windows print system can deal with.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Arguments containing the preview requested page</param>
        private static void GetPrintPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            Interlocked.Exchange(ref currentPreviewPage, e.PageNumber - 1);

            PageLoadState pageLoadState = printPreviewPages[e.PageNumber - 1];

            if (!pageLoadState.Ready)
            {
                // Notify the user that some content is not available yet
                // Apps may also opt to don't show preview untill everything is complete and just use await IsReadyAsync
                //rootPage.NotifyUser("Image loading not complete, previewing only text", NotifyType.ErrorMessage);
            }

            // Set the preview even if images failed to load properly
            printDocument.SetPreviewPage(e.PageNumber, pageLoadState.Page);
        }

        /// <summary>
        /// This is the event handler for PrintDocument.AddPages. It provides all pages to be printed, in the form of
        /// UIElements, to an instance of PrintDocument. PrintDocument subsequently converts the UIElements
        /// into a pages that the Windows print system can deal with.
        /// </summary>
        /// <param name="sender">PrintDocument</param>
        /// <param name="e">Add page event arguments containing a print task options reference</param>
        private static void AddPrintPages(object sender, AddPagesEventArgs e)
        {
            // Loop over all of the preview pages and add each one to  add each page to be printied
            for (int i = 0; i < printPreviewPages.Count; i++)
            {
                // We should have all pages ready at this point...
                printDocument.AddPage(printPreviewPages[i].Page);
            }

            // Indicate that all of the print pages have been provided
            printDocument.AddPagesComplete();
        }

        /// <summary>
        /// This function creates and adds one print preview page to the internal cache of print preview
        /// pages stored in printPreviewPages.
        /// </summary>
        /// <param name="lastRTBOAdded">Last RichTextBlockOverflow element added in the current content</param>
        /// <param name="printPageDescription">Printer's page description</param>
        private static RichTextBlockOverflow AddOnePrintPreviewPage(RichTextBlockOverflow lastRTBOAdded, PrintPageDescription printPageDescription)
        {
            // Create a cavase which represents the page 
            Canvas page = new Canvas();
            page.Width = printPageDescription.PageSize.Width;
            page.Height = printPageDescription.PageSize.Height;

            PageLoadState pageState = new PageLoadState(page, printPreviewPages.Count);
            pageState.ReadyAction = async (pageNumber, currentPage) =>
            {
                // Ignore if this is not the current page
                if (Interlocked.CompareExchange(ref currentPreviewPage, currentPreviewPage, pageNumber) == pageNumber)
                {
                    await Window.Current.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        //await new Windows.UI.Popups.MessageDialog("Content loaded").ShowAsync();
                        printDocument.SetPreviewPage(pageNumber + 1, currentPage);
                    });
                }
            };

            // Create a grid which contains the actual content to be printed
            Grid content = new Grid();

            // Get the margins size
            // If the ImageableRect is smaller than the app provided margins use the ImageableRect
            double marginWidth = Math.Max(printPageDescription.PageSize.Width - printPageDescription.ImageableRect.Width,
                                        printPageDescription.PageSize.Width * ApplicationContentMarginLeft * 2);

            double marginHeight = Math.Max(printPageDescription.PageSize.Height - printPageDescription.ImageableRect.Height,
                                         printPageDescription.PageSize.Height * ApplicationContentMarginTop * 2);

            // Set content size based on the given margins
            content.Width = printPageDescription.PageSize.Width - marginWidth;
            content.Height = printPageDescription.PageSize.Height - marginHeight;

            // Set content margins
            content.SetValue(Canvas.LeftProperty, marginWidth / 2);
            content.SetValue(Canvas.TopProperty, marginHeight / 2);

            // Add the RowDefinitions to the Grid which is a content to be printed
            RowDefinition rowDef = new RowDefinition();
            
            rowDef.Height = new GridLength(2.5, GridUnitType.Star);
            content.RowDefinitions.Add(rowDef);
            rowDef = new RowDefinition();
            rowDef.Height = new GridLength(3.5, GridUnitType.Star);
            content.RowDefinitions.Add(rowDef);
            rowDef = new RowDefinition();
            rowDef.Height = new GridLength(1.5, GridUnitType.Star);
            content.RowDefinitions.Add(rowDef);

            // If lastRTBOAdded is null then we know we are creating the first page. 
            bool isFirstPage = lastRTBOAdded == null;

            FrameworkElement previousLTCOnPage = null;
            RichTextBlockOverflow rtbo = new RichTextBlockOverflow();
            // Create the linked containers and and add them to the content grid
            if (isFirstPage)
            {
                // The first linked container in a chain of linked containers is is always a RichTextBlock
                RichTextBlock rtbl = new RichTextBlock();
                rtbl.SetValue(Grid.RowProperty, 0);
                rtbl = AddContentToRTBl(rtbl);
                int a = rtbl.Blocks.Count();
                rtbl.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                content.Children.Add(rtbl);

                // Save the RichTextBlock as the last linked container added to this page
                previousLTCOnPage = rtbl;
            }
            else
            {
                // This is not the first page so the first element on this page has to be a
                // RichTextBoxOverflow that links to the last RichTextBlockOverflow added to
                // the previous page.
                rtbo = new RichTextBlockOverflow();
                rtbo.SetValue(Grid.RowProperty, 0);
                content.Children.Add(rtbo);

                // Keep text flowing from the previous page to this page by setting the linked text container just
                // created (rtbo) as the OverflowContentTarget for the last linked text container from the previous page 
                lastRTBOAdded.OverflowContentTarget = rtbo;

                // Save the RichTextBlockOverflow as the last linked container added to this page
                previousLTCOnPage = rtbo;
            }

            if (ShowText)
            {
                // Create the next linked text container for on this page.
                rtbo = new RichTextBlockOverflow();
                rtbo.SetValue(Grid.RowProperty, 1);

                // Add the RichTextBlockOverflow to the content to be printed.
                content.Children.Add(rtbo);

                // Add the new RichTextBlockOverflow to the chain of linked text containers. To do this we much check
                // to see if the previous container is a RichTextBlock or RichTextBlockOverflow.
                if (previousLTCOnPage is RichTextBlock)
                    ((RichTextBlock)previousLTCOnPage).OverflowContentTarget = rtbo;
                else
                    ((RichTextBlockOverflow)previousLTCOnPage).OverflowContentTarget = rtbo;

                // Save the last linked text container added to the chain
                previousLTCOnPage = rtbo;

                // Create the next linked text container for on this page.
                rtbo = new RichTextBlockOverflow();
                rtbo.SetValue(Grid.RowProperty, 2);
                content.Children.Add(rtbo);

                // Add the new RichTextBlockOverflow to the chain of linked text containers. We don't have to check
                // the type of the previous linked container this time because we know it's a RichTextBlockOverflow element
                ((RichTextBlockOverflow)previousLTCOnPage).OverflowContentTarget = rtbo;
            }
            // We are done creating the content for this page. Add it to the Canvas which represents the page
            page.Children.Add(content);

            // Add the newley created page to the printing root which is part of the visual tree and force it to go
            // through layout so that the linked containers correctly distribute the content inside them.
            PrintingRoot.Children.Add(page);
            PrintingRoot.InvalidateMeasure();
            PrintingRoot.UpdateLayout();

            // Add the newley created page to the list of pages
            printPreviewPages.Add(pageState);

            // Return the last linked container added to the page
            return rtbo;
        }

        /// <summary>
        /// This function adds content to the blocks collection of the RichTextBlock passed into the function.      
        /// </summary>
        /// <param name="rtbl">last rich text block</param>
        private static RichTextBlock AddContentToRTBl(RichTextBlock rtbl)
        {
            // Create a Run and give it content
            Run run = new Run();
            run.Text = PrintingTextBox.Text;

            // Create a paragraph, set it's property according to text box.
            Paragraph para = new Paragraph();
            para.CharacterSpacing = PrintingTextBox.CharacterSpacing;
            para.FontFamily = PrintingTextBox.FontFamily;
            para.FontSize = PrintingTextBox.FontSize;
            para.FontStretch = PrintingTextBox.FontStretch;
            para.FontStyle = PrintingTextBox.FontStyle;
            para.FontWeight = PrintingTextBox.FontWeight;
            para.Foreground = PrintingTextBox.Foreground;
            para.Language = PrintingTextBox.Language;
            para.TextAlignment = PrintingTextBox.TextAlignment;

            para.Inlines.Add(run);
            // Add the paragraph to the blocks collection of the RichTextBlock
            rtbl.Blocks.Add(para);
            return rtbl;
        }
        
        /// <summary>
        /// Shows device charm with the print preview.
        /// </summary>
        /// <param name="PrintingCanvas">Object of canvas class for showing print preview.</param>
        /// <param name="TextboxToBePrint">The textbox of which the text is to be printed.</param>
        /// <param name="FileName">The file name for the document which is going to be print.</param>
        public static async Task ShowPrintUIAsync(Canvas PrintingCanvas, TextBox TextboxToBePrint, string FileName)
        {
            if (PrintingCanvas == null)
                throw new NullReferenceException("Canvas must be present in XAML. It is for showing print preview.");

            if (TextboxToBePrint == null)
                throw new NullReferenceException("Textbox is primary requirement to print the text.");

            if (string.IsNullOrWhiteSpace(FileName))
                throw new ArgumentException("The file name for the document which is going to be print must not be empty or only white space.");

            PrintingRoot = PrintingCanvas;
            PrintingTextBox = TextboxToBePrint;
            PrintingFileName = FileName;
            UnregisterForPrinting();
            RegisterForPrinting();
            await PrintManager.ShowPrintUIAsync();
        }
    }

    /// <summary>
    /// Helpers class that tracks the loading state of a xaml element
    /// Elements which use async load patterns must enlist for observation
    /// </summary>
    public class PageLoadState
    {
        /// <summary>
        /// Event used to detect when all "loadable" elements are ready
        /// </summary>
        private CountdownEvent loadingElements;

        /// <summary>
        /// An action to execute when content is available (eg: SetPreview for the page)
        /// </summary>
        public Action<int, UIElement> ReadyAction { get; set; }

        /// <summary>
        /// Current page number in print/preview list
        /// </summary>
        private int pageNumber;

        /// <summary>
        /// XAML Page(element)
        /// </summary>
        private UIElement page;

        public UIElement Page
        {
            get { return page; }
        }

        public PageLoadState(UIElement page, int pageNumber)
        {
            this.page = page;
            this.pageNumber = pageNumber;
            loadingElements = new CountdownEvent(0);
        }

        /// <summary>
        /// Internal method that is called when an element has finished loading
        /// </summary>        
        private void SetElementComplete()
        {
            loadingElements.Signal();
        }

        /// <summary>
        /// Adds an element in the observation list
        /// </summary>
        /// <param name="bitmap">The bitmap on which to listen for ImageOpened event</param>
        public void ListenForCompletion(BitmapImage bitmap)
        {
            if (loadingElements.CurrentCount == 0)
            {
                // Event is already signaled. Manually set the count to 1 and "arm" the event.
                loadingElements.Reset(1);
            }
            else
            {
                // AddCount will throw if event is already in signaled state.
                loadingElements.AddCount();
            }
            bitmap.ImageOpened += (s, e) => SetElementComplete();
        }

        /// <summary>
        /// Property used to determine if the content is ready
        /// If content is not ready a background task will serve the content once it's ready
        /// </summary>
        public bool Ready
        {
            get
            {
                var ready = loadingElements.CurrentCount == 0;
                if (!ready)
                {
                    // A request was made and the content is not ready, serve it once it's complete
                    Task.Run(async () =>
                    {
                        await IsReadyAsync();
                        ReadyAction(pageNumber, page);
                    });
                }

                return ready;
            }
        }

        /// <summary>
        /// Async method that enables listening for the completion event in a background thread
        /// </summary>
        public async Task IsReadyAsync()
        {
            await Task.Run(() => { loadingElements.Wait(); });
        }
    }

    internal enum DisplayContent : int
    {
        /// <summary>
        /// Show only text
        /// </summary>
        Text = 1,

        /// <summary>
        /// Show only images
        /// </summary>
        Images = 2,

        /// <summary>
        /// Show a combination of images and text
        /// </summary>
        TextAndImages = 3
    }
}
