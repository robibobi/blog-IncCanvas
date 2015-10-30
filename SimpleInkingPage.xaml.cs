using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;


namespace Tcoc.InkDrawingSample
{
    public sealed partial class SimpleInkingPage : Page
    {
        private WriteableBitmap mImage;

        public SimpleInkingPage()
        {
            this.InitializeComponent();
            this.OpenImageButton.Click += OpenImageBtnClicked;
            this.SaveImageButton.Click += SaveImageButtonClicked;
            InitializeInkCanvas();
        }

        private void InitializeInkCanvas()
        {
            TheInkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse
                | Windows.UI.Core.CoreInputDeviceTypes.Pen
                | Windows.UI.Core.CoreInputDeviceTypes.Touch;

            InkDrawingAttributes da = TheInkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            da.Size = new Windows.Foundation.Size(3, 3);
            da.Color = Colors.Red;
            TheInkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(da);
        }

        private async void OpenImageBtnClicked(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".bmp");
            StorageFile f = await picker.PickSingleFileAsync();
            if (f != null)
            {
                Stream imgStream = (await f.OpenReadAsync()).AsStream();
                mImage = await new WriteableBitmap(1, 1).FromStream(imgStream);
                TheImage.Source = mImage;
            }
        }

        private async void SaveImageButtonClicked(object sender, RoutedEventArgs e)
        {
            await SaveStrokesToBitmap(mImage);
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            savePicker.FileTypeChoices.Add("bmp Image", new List<string>() { ".bmp" });
            savePicker.SuggestedFileName = "inkImage";
            StorageFile f = await savePicker.PickSaveFileAsync();
            if (f != null)
            {
                using (Stream stream = await f.OpenStreamForWriteAsync())
                {
                    await mImage.ToStream(stream.AsRandomAccessStream(), BitmapEncoder.BmpEncoderId);
                }
            }
        }

        private async Task SaveStrokesToBitmap(WriteableBitmap b)
        {
            Rect imgRect = new Rect(0, 0, b.PixelWidth, b.PixelHeight);
            InkStrokeContainer container = TheInkCanvas.InkPresenter.StrokeContainer;
            InkStrokeBuilder builder = new InkStrokeBuilder();

            // Unsichtbare Tinte!
            InkDrawingAttributes da = TheInkCanvas.InkPresenter.CopyDefaultDrawingAttributes();
            da.Size = new Size(0.1, 0.1);
            builder.SetDefaultDrawingAttributes(da);

            // Strich in oberer linker Ecke einfügen
            InkStroke topLeft = builder.CreateStroke(new List<Point>() {
                new Point(1, 1),
                new Point(2, 2) });
            container.AddStroke(topLeft);

            // Strich in unterer Rechter Ecke einfügen
            InkStroke bottomRight = builder.CreateStroke(new List<Point>() {
                new Point(imgRect.Width -2, imgRect.Height -2),
                new Point(imgRect.Width -1, imgRect.Height -1) });   
            container.AddStroke(bottomRight);

            // Striche in WriteableBitmap speichern
            WriteableBitmap bmp;
            using (InMemoryRandomAccessStream ims =
                new InMemoryRandomAccessStream())
            {
                await container.SaveAsync(ims);
                bmp = await new WriteableBitmap(1, 1)
                    .FromStream(ims, BitmapPixelFormat.Bgra8);
            }
            // Bilder zusammenfügen
            b.Blit(imgRect, bmp, imgRect, WriteableBitmapExtensions.BlendMode.Alpha);
        }


    }
}
