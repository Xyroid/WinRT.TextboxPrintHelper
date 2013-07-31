WinRT.TextboxPrintHelper
========================

Microsoft's documentation &amp; sample for printing is too much complex. The sample app for printing shows only how to print RichTextBlock. There's no tutorial to print the text box content in Windows Store app. Moreover to print single line of string, one has to write too much code rathar than maximum 10 lines in WinForm. So this library provides a helper class which has only one static method. It prints the text box content with its formatting. It's majorely based on MSDN print sample.

I got idea to write a library from the following Stack Overflow questions.

[How to print the contents of a TextBox](http://stackoverflow.com/questions/15563886/how-to-print-the-contents-of-a-textbox)

[How do you print contents of a RichTextBox?](http://stackoverflow.com/questions/17894817/how-do-you-print-contents-of-a-richtextbox)

[How do I print a TextFile OR contents of a TextBox in Metro apps?](http://stackoverflow.com/questions/15595706/how-do-i-print-a-textfile-or-contents-of-a-textbox-in-metro-apps)

Demo
========================

You have to call just one static method, it will do print job on your behalf.

`await Printer.ShowPrintUIAsync(PrintPreview, MyTextBox, Guid.NewGuid().ToString());`

PrintPreview is a canvas, which is mandatory to show print preview in device charm. You have to declare <Canvas /> in your XAML.
MyTextBox is a text box object, whose text content is going to be printed.
Guid.NewGuid().ToString() is a file name for printing the document. (It will be applied if you are creating PDF or XPS file via printing.)

You can find NuGet package [here](https://www.nuget.org/packages/WinRT.TextboxPrintHelper/)

I would be glad to have issues or suggestions from you, so I can improve this. Ping me on my twitter handle [\_F4RH4N\_](https://twitter.com/_F4RH4N_).
